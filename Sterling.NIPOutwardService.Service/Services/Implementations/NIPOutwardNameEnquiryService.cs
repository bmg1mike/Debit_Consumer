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
    private readonly APISettings apiSettings;
    private InboundLog inboundLog;
    private readonly IInboundLogService inboundLogService;


    public NIPOutwardNameEnquiryService(ISSM ssm, IOptions<AppSettings> appSettings, IOptions<APISettings> apiSettings,
        IInboundLogService inboundLogService)
    {
        this.ssm = ssm;
        this.appSettings = appSettings.Value;
        this.apiSettings = apiSettings.Value;
        this.inboundLog = new InboundLog {
            InboundLogId = ObjectId.GenerateNewId().ToString(), 
            OutboundLogs = new List<OutboundLog>(),
            };
        this.inboundLogService = inboundLogService;
    }

    public async Task<Result<NameEnquiryResponseDto>> DoNameEnquiry(NameEnquiryRequestDto request)
    {
        var response = new Result<NameEnquiryResponseDto>();
        inboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        inboundLog.APICalled = "NIPOutwardService";
        inboundLog.APIMethod = "NameEnquiry";
        inboundLog.RequestDetails = JsonConvert.SerializeObject(request);

        var validationResult = ValidateNameEnquiryResponseDto(request);

        if(!validationResult.IsSuccess)
        {
            inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            inboundLog.ResponseDetails = validationResult.ErrorMessage;
            await inboundLogService.CreateInboundLog(inboundLog);
            return validationResult;
        }

        //string msg = "";
        //string responsecodeVal = "";
        //string AcctNameval = "";
        
        createRequest(request);

        if (!sendRequest()) //unsuccessful request
        {
            response.Message = "Name enquiry failed";
            response.IsSuccess = false;
            //AcctNameval = "";
            // msg = responsecodeVal + ":" + AcctNameval;
            //new ErrorLog(msg);
        }
        else
        {
            var nameEnquiryResponse = readResponse();

            if(nameEnquiryResponse.ResponseCode == "00") 
            {
                response.Content = nameEnquiryResponse;
                response.Message = "Success";
                response.IsSuccess = true;
            }
            else
            {
                response.Message = "Name enquiry failed";
                response.IsSuccess = false;
            }

            // responsecodeVal = sne.ResponseCode;
            // AcctNameval = sne.AccountName;
            // msg = responsecodeVal + ":" + AcctNameval;
            // new ErrorLog(msg);
        }
        inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        inboundLog.ResponseDetails = JsonConvert.SerializeObject(response);
        await inboundLogService.CreateInboundLog(inboundLog);
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
            result.Message = sb.ToString();

           
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
        try
        {
            BasicHttpBinding binding = new()
            {
                CloseTimeout = TimeSpan.FromMinutes(apiSettings.NIBSSNIPServiceCloseTimeoutInMinutes),
                OpenTimeout = TimeSpan.FromMinutes(apiSettings.NIBSSNIPServiceOpenTimeoutInMinutes),
                ReceiveTimeout = TimeSpan.FromMinutes(apiSettings.NIBSSNIPServiceReceiveTimeoutInMinutes),
                SendTimeout = TimeSpan.FromMinutes(apiSettings.NIBSSNIPServiceSendTimeoutInMinutes),
                MaxBufferPoolSize = apiSettings.NIBSSNIPServiceMaxBufferPoolSize,
                MaxReceivedMessageSize = apiSettings.NIBSSNIPServiceMaxReceivedMessageSize
            };

            NIPInterfaceClient nipClient = new NIPInterfaceClient(binding, new EndpointAddress(apiSettings.NIPNIBSSService));
           
            string str = ssm.Encrypt(rqt.ToString());
            xml = nipClient.nameenquirysingleitem(str);
            
            ok = true;
        }
        catch (Exception)
        {
            //Mylogger1.Error(ex);
            ok = false;
            //ResponseCode = "96";
        }
        return ok;
    }

    public NameEnquiryResponseDto readResponse()
    {
        NameEnquiryResponseDto response = new NameEnquiryResponseDto();
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
        catch (Exception)
        {
            //new ErrorLog(ex);
            response.ResponseCode = "96";
            //return false;
        }
        return response;
    }
}