using Serilog;

namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public class NIPOutwardNameEnquiryService : INIPOutwardNameEnquiryService
{
    StringBuilder rqt = new StringBuilder();
    StringBuilder rsp = new StringBuilder();
    public string AccountName;
    public string ResponseCode;

    public string xml;
    private readonly ISSM ssm;
    private readonly AppSettings appSettings;
    private readonly NibssNipServiceProperties nibssNipServiceProperties;
    private InboundLog inboundLog;
    private readonly IInboundLogService inboundLogService;
    private readonly INIPOutwardNameEnquiryRepository nipOutwardNameEnquiryRepository;
    private readonly IHttpContextAccessor httpContextAccessor;

    private readonly AsyncRetryPolicy retryPolicy;
    private List<OutboundLog> outboundLogs;

    public NIPOutwardNameEnquiryService(ISSM ssm, IOptions<AppSettings> appSettings, IOptions<NibssNipServiceProperties> nibssNipServiceProperties,
        IInboundLogService inboundLogService, INIPOutwardNameEnquiryRepository nipOutwardNameEnquiryRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        this.ssm = ssm;
        this.appSettings = appSettings.Value;
        this.nibssNipServiceProperties = nibssNipServiceProperties.Value;
        this.inboundLog = new InboundLog {
            InboundLogId = ObjectId.GenerateNewId().ToString(), 
            OutboundLogs = new List<OutboundLog>(),
            };
        this.outboundLogs = new List<OutboundLog> ();
        this.inboundLogService = inboundLogService;
        this.httpContextAccessor = httpContextAccessor;
        this.nipOutwardNameEnquiryRepository = nipOutwardNameEnquiryRepository;
        this.retryPolicy = Policy.Handle<Exception>()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        }, (exception, timeSpan, retryCount, context) =>
        {
            var outboundLog = new OutboundLog  { OutboundLogId = ObjectId.GenerateNewId().ToString() };
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + "\r\n" + @$"Retrying due to {exception.GetType().Name}... Attempt {retryCount}
                at {DateTime.UtcNow.AddHours(1)} Exception Details: {exception.Message} {exception.StackTrace} " ;
            outboundLogs.Add(outboundLog);
        });
    }

    public async Task<Result<NameEnquiryResponseDto>> DoNameEnquiry(NameEnquiryRequestDto request)
    {
        var response = new Result<NameEnquiryResponseDto>();

        try
        {
            var requestTime = DateTime.UtcNow.AddHours(1);
            inboundLog.RequestDateTime = requestTime;
            inboundLog.APICalled = "NIPOutwardService";
            inboundLog.APIMethod = "NameEnquiry";
            inboundLog.RequestDetails = JsonConvert.SerializeObject(request);
            inboundLog.RequestSystem = httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();

            Log.Information("Before Calling Process Name Enquiry");
            response = await ProcessNameEnquiry(request);
            Log.Information("After Calling Process Name Enquiry");


            response.SessionID = request.SessionID;
            response.RequestTime = requestTime;
            response.ResponseTime = DateTime.UtcNow.AddHours(1);
            inboundLog.ResponseDetails = JsonConvert.SerializeObject(response);
            inboundLog.ResponseDateTime = response.ResponseTime;
            inboundLog.OutboundLogs = outboundLogs;

            //await inboundLogService.CreateInboundLog(inboundLog);
            Task.Run(() => LogToMongoDb(inboundLog));
        }
        catch (System.Exception ex)
        {
            response.IsSuccess = false;
            response.ResponseTime = DateTime.UtcNow.AddHours(1);
            response.Content = null;
            response.Message = "Transaction failed";
            response.ErrorMessage = "Internal server error";
            Log.Information(ex, $"Error thrown, raw request: {JsonConvert.SerializeObject(request)} ");
        }
        
        return response;
    }

    public void LogToMongoDb(InboundLog log)
    {
        inboundLogService.CreateInboundLog(inboundLog);
        Thread.Sleep(1000);
    }

    public async Task<Result<NameEnquiryResponseDto>> ProcessNameEnquiry(NameEnquiryRequestDto request)
    {
        var validationResult = ValidateNameEnquiryResponseDto(request);

        if(!validationResult.IsSuccess)
        {
            return validationResult;
        }

        return await NameEnquiry(request);;
    }

    public async Task<Result<NameEnquiryResponseDto>> NameEnquiry(NameEnquiryRequestDto request)
    {
        var response = new Result<NameEnquiryResponseDto>();

        var nameEnquiryDetailsResult = new NIPOutwardNameEnquiry();
        await retryPolicy.ExecuteAsync(async () =>
        {
            
            nameEnquiryDetailsResult = await nipOutwardNameEnquiryRepository
            .Get(request.DestinationInstitutionCode, request.AccountNumber);
        });

        

        var nameEnquiryResponse = new NameEnquiryResponseDto();
        if (nameEnquiryDetailsResult != null && nameEnquiryDetailsResult.ResponseCode == "00")
        {
            nameEnquiryResponse.AccountName = nameEnquiryDetailsResult.AccountName;
            nameEnquiryResponse.AccountNumber = nameEnquiryDetailsResult.AccountNumber;
            nameEnquiryResponse.BankVerificationNumber = nameEnquiryDetailsResult.BVN;
            nameEnquiryResponse.DestinationInstitutionCode = nameEnquiryDetailsResult.DestinationInstitutionCode;
            nameEnquiryResponse.ChannelCode = request.ChannelCode;
            nameEnquiryResponse.KYCLevel = nameEnquiryDetailsResult.KYCLevel;
            nameEnquiryResponse.SessionID = request.SessionID;
            nameEnquiryResponse.ResponseCode = nameEnquiryDetailsResult.ResponseCode;

            response.Content = nameEnquiryResponse;
            response.Message = "Success";
            response.IsSuccess = true;

            return response;
        }
        
        
        createRequest(request);

        if (!sendRequest()) //unsuccessful request
        {
            response.Message = "Name enquiry failed";
            response.IsSuccess = false;
           
        }
        else
        {
            nameEnquiryResponse = readResponse();

            if(nameEnquiryResponse.ResponseCode == "00") 
            {
                var nipOutwardNameEnquiry = new NIPOutwardNameEnquiry 
                {
                    ResponseCode = nameEnquiryResponse.ResponseCode,
                    SessionID = nameEnquiryResponse.SessionID,
                    AccountName = nameEnquiryResponse.AccountName,
                    BVN = nameEnquiryResponse.BankVerificationNumber,
                    KYCLevel = nameEnquiryResponse.KYCLevel,
                    AccountNumber = nameEnquiryResponse.AccountNumber,
                    DestinationInstitutionCode = nameEnquiryResponse.DestinationInstitutionCode,
                    ChannelCode = nameEnquiryResponse.ChannelCode,
                    DateAdded = DateTime.UtcNow.AddHours(1),
                };

                try
                {
                    await nipOutwardNameEnquiryRepository.Create(nipOutwardNameEnquiry);
                }
                catch (System.Exception ex)
                {
                    var outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
                    outboundLog.ExceptionDetails = @$"Error thrown while attempting to add name enquiry record to table:
                    {ex.Message} {ex.StackTrace}";
                    outboundLogs.Add(outboundLog);
                }

                response.Content = nameEnquiryResponse;
                response.Message = "Success";
                response.IsSuccess = true;
            }
            else
            {
                response.Message = "Name enquiry failed";
                response.IsSuccess = false;
            }

        }
        
        return response;
    }

    public Result<NameEnquiryResponseDto> ValidateNameEnquiryResponseDto(NameEnquiryRequestDto request)
    {
        Result<NameEnquiryResponseDto> result = new Result<NameEnquiryResponseDto>();
        result.IsSuccess = false;

        NameEnquiryRequestDtoValidator validator = new NameEnquiryRequestDtoValidator();
            ValidationResult results = validator.Validate(request);

        if (!results.IsValid)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var failure in results.Errors)
            {
                sb.Append("Property " + failure.PropertyName + " failed validation. Error was: " + failure.ErrorMessage);
            }

            result.IsSuccess = false;
            result.ErrorMessage = sb.ToString();
            result.Message = "Invalid request";

           
        }
        else{
             result.IsSuccess = true;
        }

        return result;
    }

    public void createRequest(NameEnquiryRequestDto request)
    {
        rqt.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        rqt.Append("<NESingleRequest>");
        rqt.Append("<SessionID>" + request.SessionID + "</SessionID>");
        rqt.Append("<DestinationInstitutionCode>" + request.DestinationInstitutionCode + "</DestinationInstitutionCode>");
        rqt.Append("<ChannelCode>" + request.ChannelCode + "</ChannelCode>");
        rqt.Append("<AccountNumber>" + request.AccountNumber + "</AccountNumber>");
        rqt.Append("</NESingleRequest>");
    }
    public bool sendRequest()
    {
        bool ok = false;
        var outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        var request = rqt.ToString();
        try
        {
            outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.APICalled = nibssNipServiceProperties.NIPNIBSSService;
            outboundLog.RequestDetails = request;

            BasicHttpBinding binding = new()
            {
                CloseTimeout = TimeSpan.FromMinutes(nibssNipServiceProperties.NIBSSNIPServiceCloseTimeoutInMinutes),
                OpenTimeout = TimeSpan.FromMinutes(nibssNipServiceProperties.NIBSSNIPServiceOpenTimeoutInMinutes),
                ReceiveTimeout = TimeSpan.FromMinutes(nibssNipServiceProperties.NIBSSNIPServiceReceiveTimeoutInMinutes),
                SendTimeout = TimeSpan.FromMinutes(nibssNipServiceProperties.NIBSSNIPServiceSendTimeoutInMinutes),
                MaxBufferPoolSize = nibssNipServiceProperties.NIBSSNIPServiceMaxBufferPoolSize,
                MaxReceivedMessageSize = nibssNipServiceProperties.NIBSSNIPServiceMaxReceivedMessageSize
            };

            NIPInterfaceClient nipClient = new NIPInterfaceClient(binding, new EndpointAddress(nibssNipServiceProperties.NIPNIBSSService));
           
            string str = ssm.Encrypt(request);
            Log.Information($"Nibbs Name Enquiry Request \n {str} - {request}");
            xml = nipClient.nameenquirysingleitem(str);

            Log.Information($"Nibbs Name Enquiry Response \n {xml}");
            
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            ok = true;
        }
        catch (Exception ex)
        {
            ok = false;
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            //ResponseCode = "96";
        }
        outboundLog.ResponseDetails = $"Name enquiry call successful: {ok}";
        outboundLogs.Add(outboundLog);
        return ok;
    }

    public NameEnquiryResponseDto readResponse()
    {
        NameEnquiryResponseDto response = new NameEnquiryResponseDto();
        var outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.readResponse)}";

        try
        {
            //new ErrorLog("Read Inward NE " + xml);
            //SSM ssm = new SSM();

            xml = ssm.Decrypt(xml);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            response.SessionID = xmlDoc.GetElementsByTagName("SessionID").Item(0).InnerText;
            response.DestinationInstitutionCode = xmlDoc.GetElementsByTagName("DestinationInstitutionCode").Item(0).InnerText;
            byte ChannelCode = 0;
            var ChannelCodeValid = byte.TryParse(xmlDoc.GetElementsByTagName("ChannelCode").Item(0).InnerText, out ChannelCode);
            response.ChannelCode = ChannelCode;
            response.AccountNumber = xmlDoc.GetElementsByTagName("AccountNumber").Item(0).InnerText;
            response.AccountName = xmlDoc.GetElementsByTagName("AccountName").Item(0).InnerText;
            //clean account name
            //this.AccountName = this.AccountName.Replace("&", "&amp;");
            response.AccountName = response.AccountName.Replace("'", "&apos;");
            response.AccountName = response.AccountName.Replace("\"", "&quot;");

            response.BankVerificationNumber = xmlDoc.GetElementsByTagName("BankVerificationNumber").Item(0).InnerText;
            response.KYCLevel = xmlDoc.GetElementsByTagName("KYCLevel").Item(0).InnerText;
            response.ResponseCode = xmlDoc.GetElementsByTagName("ResponseCode").Item(0).InnerText;
        
            //return true;
        }
        catch (Exception ex)
        {
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {xml} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            response.ResponseCode = "96";
            //return false;
        }
        outboundLog.ResponseDetails = xml;
        outboundLogs.Add(outboundLog);

        return response;
    }

    public List<OutboundLog> GetOutboundLogs()
    {
        var recordsToBeMoved = this.outboundLogs;
        this.outboundLogs = new List<OutboundLog>();
        return recordsToBeMoved;
    }
}