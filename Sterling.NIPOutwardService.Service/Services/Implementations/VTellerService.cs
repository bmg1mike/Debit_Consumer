using Sterling.NIPOutwardService.Service.Helpers;

namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public class VTellerService : IVtellerService
{
    public string Fee_expl_code = "937";
    public string TSS_bra_code = "";
    public string TSS_cus_num = "";
    public string TSS_cur_code = "";
    public string TSS_led_code = "";
    public string TSS_sub_acct_code = "";

    public string FEE_bra_code = "";
    public string FEE_cus_num = "";
    public string FEE_cur_code = "";
    public string FEE_led_code = "";
    public string FEE_sub_acct_code = "";

    public string RespCreditedamt;
    public string Respreturnedcode1;
    public string Respreturnedcode2;

    public string Prin_Rsp;
    public string Fee_Rsp;
    public string Vat_Rsp;

    public string ResponseMsg;

    public decimal NIPfee;
    public decimal NIPvat;
    public string error_text;
    
    private readonly ITransactionDetailsRepository transactionDetailsRepository;
    private List<OutboundLog> outboundLogs;
    private readonly AppSettings appSettings;

    public VTellerService(ITransactionDetailsRepository transactionDetailsRepository, IOptions<AppSettings> appSettings)
    {
        this.transactionDetailsRepository = transactionDetailsRepository;
        this.outboundLogs = new List<OutboundLog> ();
        this.appSettings = appSettings.Value;

    }
    public List<OutboundLog> GetOutboundLogs()
    {
        var recordsToBeMoved = this.outboundLogs;
        this.outboundLogs = new List<OutboundLog>();
        return recordsToBeMoved;
    }
    public async Task<VTellerResponseDto> authorizeIBSTrnxFromSterling(CreateVTellerTransactionDto t, IncomeAccountsDetails incomeAccountsDetails)
    {
        var vTellerResponse = new VTellerResponseDto();
        RespCreditedamt = "x";
        vTellerResponse.Respreturnedcode1 = "x";
        Respreturnedcode2 = "x";

        string expl_code = "";

        //DataSet dsExp = ts.getExpcode();
        var dsExp = incomeAccountsDetails.ExpCode;
        if (!string.IsNullOrWhiteSpace(dsExp))
        {
            expl_code = dsExp;
        }
        else
        {
        }

        string TSSAcct = ""; int Last4 = 0;
        var dsTss = incomeAccountsDetails.Tss;// TssAccountsAndCode.Tss;
        var dsFee = incomeAccountsDetails.Fee; 
        //assign the Tss account to the varriables

        bool foundval = await transactionDetailsRepository.isBankCodeFound(t.inCust.bra_code);
        this.outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

        if (foundval)
        {
            //get TSS account based on the branch code of the transacting account
            var dsTss1 = incomeAccountsDetails.Tss2;// TssAccountsAndCode.Tss1;
            if (!string.IsNullOrWhiteSpace(dsTss1))
            {
                TSS_bra_code = "NG0020001";
                TSS_cus_num = "";
                TSS_cur_code = "NGN";
                TSS_led_code = "12501";
                Last4 = int.Parse(TSS_bra_code.Substring(6, 3)) + 2000;
                TSSAcct = "NGN" + TSS_led_code + "0001" + Last4.ToString();
                TSS_sub_acct_code = TSSAcct;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(dsTss))
            {
                TSS_bra_code = "NG0020001"; 
                TSS_cus_num = "";
                TSS_cur_code = "NGN";
                TSS_led_code = "12501";
                Last4 = int.Parse(TSS_bra_code.Substring(6, 3)) + 2000;
                TSSAcct = "NGN" + TSS_led_code + "0001" + Last4.ToString();
                TSS_sub_acct_code = TSSAcct;
            }
        }

        //assign the income account to the variable
        //to enusre USSD fees for NIP goes into a separate account PL52259
        // while sterling momey, sterling one and IBS goes into another. PL52340 ,
        // PL for NIP_PL_ACCT_CIB
        string NIP_PL_ACCT_USSD = appSettings.NIP_PL_ACCT_USSD;
        string NIP_PL_ACCT_OTHERS = appSettings.NIP_PL_ACCT_OTHERS;
        string NIP_PL_ACCT_CIB = appSettings.NIP_PL_ACCT_CIB;
        string NIP_PL_ACCT_WHATSAPP = appSettings.NIP_PL_ACCT_WHATSAPP;
        string NIP_PL_ACCT_CHATPAY = appSettings.NIP_PL_ACCT_CHATPAY;
       
        if (foundval)
        {
            if (dsFee != null)
            {
                FEE_bra_code = dsFee.bra_code;
                FEE_cus_num = dsFee.cusnum;
                FEE_cur_code = dsFee.curcode;
                FEE_led_code = dsFee.ledcode;
                FEE_sub_acct_code = dsFee.subacctcode;

                if (t.Appid == 26)
                {
                    FEE_bra_code = "NG0020001";
                    FEE_cus_num = "";
                    FEE_cur_code = "NGN";
                    FEE_led_code = "52259";
                    TSSAcct = NIP_PL_ACCT_USSD;
                    FEE_sub_acct_code = TSSAcct;
                }
                else if (t.Appid == 31)
                {
                    FEE_bra_code = "NG0020001";
                    FEE_cus_num = "";
                    FEE_cur_code = "NGN";
                    FEE_led_code = "52522";
                    TSSAcct = NIP_PL_ACCT_CIB;
                    FEE_sub_acct_code = TSSAcct;
                }
                else if (t.Appid == 78)
                {
                    FEE_bra_code = "NG0020001";
                    FEE_cus_num = "";
                    FEE_cur_code = "NGN";
                    FEE_led_code = "52301";
                    TSSAcct = NIP_PL_ACCT_CHATPAY;
                    FEE_sub_acct_code = TSSAcct;
                }
                else if (t.Appid == 2)
                {
                    FEE_bra_code = "NG0020001";
                    FEE_cus_num = "";
                    FEE_cur_code = "NGN";
                    FEE_led_code = "52864";
                    TSSAcct = NIP_PL_ACCT_WHATSAPP;
                    FEE_sub_acct_code = TSSAcct;
                }
                else
                {
                    FEE_bra_code = "NG0020001";
                    FEE_cus_num = "";
                    FEE_cur_code = "NGN";
                    FEE_led_code = "52340";
                    TSSAcct = NIP_PL_ACCT_OTHERS;
                    FEE_sub_acct_code = TSSAcct;
                }
            }
        }
        else
        {
            if (dsFee != null)
            {
                FEE_bra_code = "NG0020001";
                FEE_cus_num = dsFee.cusnum;
                FEE_cur_code = dsFee.curcode;
                FEE_led_code = dsFee.ledcode;
                FEE_sub_acct_code = dsFee.subacctcode;

                if (t.Appid == 26)
                {
                    FEE_bra_code = "NG0020001";
                    FEE_cus_num = "";
                    FEE_cur_code = "NGN";
                    FEE_led_code = "52259";
                    TSSAcct = NIP_PL_ACCT_USSD;
                    FEE_sub_acct_code = TSSAcct;
                }
                else if (t.Appid == 31)
                {
                    FEE_bra_code = "NG0020001";
                    FEE_cus_num = "";
                    FEE_cur_code = "NGN";
                    FEE_led_code = "52522";
                    TSSAcct = NIP_PL_ACCT_CIB;
                    FEE_sub_acct_code = TSSAcct;
                }
                else if (t.Appid == 78)
                {
                    FEE_bra_code = "NG0020001";
                    FEE_cus_num = "";
                    FEE_cur_code = "NGN";
                    FEE_led_code = "52301";
                    TSSAcct = NIP_PL_ACCT_CHATPAY;
                    FEE_sub_acct_code = TSSAcct;
                }
                else if (t.Appid == 2)
                {
                    FEE_bra_code = "NG0020001";
                    FEE_cus_num = "";
                    FEE_cur_code = "NGN";
                    FEE_led_code = "52864";
                    TSSAcct = NIP_PL_ACCT_WHATSAPP;
                    FEE_sub_acct_code = TSSAcct;
                }
                else
                {
                    FEE_bra_code = "NG0020001";
                    FEE_cus_num = "";
                    FEE_cur_code = "NGN";
                    FEE_led_code = "52340";
                    TSSAcct = NIP_PL_ACCT_OTHERS;
                    FEE_sub_acct_code = TSSAcct;
                }
            }
        }

        string xrem = "";

        //log.Info("appid " + t.Appid.ToString());

        if (t.Appid == 5)
        {
            xrem = t.PaymentReference;
        }
        else
        {
            xrem = t.PaymentReference;
        }
        xrem = xrem.Replace("&amp;", "&");
        xrem = xrem.Replace("&apos;", "'");
        xrem = xrem.Replace("&quot;", "\"");

        xrem = xrem.Replace("& ", "&amp;");
        xrem = xrem.Replace("&", "&amp;");
        xrem = xrem.Replace("'", "&apos;");
        xrem = xrem.Replace("\"", "&quot;");


        // string xremark1 = "NIP/" + xrem;
        string xremark1 = xrem;
        string xremark2 = "NIPFEE/" + xrem;
        string xremark3 = "NIPVAT/" + xrem;

        ////ensure the remarks are not more than 136
        if (xremark1.Length > 136)
        {
            xremark1 = xremark1.Substring(0, 136);
        }

        if (xremark2.Length > 136)
        {
            xremark2 = xremark2.Substring(0, 136);
        }

        if (xremark3.Length > 136)
        {
            xremark3 = xremark3.Substring(0, 136);
        }

        //before going to T24, check if the bracode is a switch bracode
        if (t.inCust.bra_code == "NG0020556")
        {
            //read the switchfee from config file
            decimal SWITCHNIPFEE = appSettings.SWITCHNIPFEE;
            //compute the VAT (5% of NIP fee) 
            decimal computedVat = decimal.Parse("0.05") * SWITCHNIPFEE;
            //assign the new fee for fee and vat
            t.feecharge = SWITCHNIPFEE;
            t.vat = computedVat;
        }

        //CHECK IF THE APPID IS FOR FLUTTERWAVE (56), ZDVANCE (99), kUDI.ai (1112)
        if (t.Appid == 56) //flutterwave
        {
            decimal FLUTTERWAVE_FEE = appSettings.FLUTTERWAVE_FEE;
            //compute the VAT (5% of NIP fee) 
            decimal computedVat = decimal.Parse("0.05") * FLUTTERWAVE_FEE;
            //assign the new fee for fee and vat
            t.feecharge = FLUTTERWAVE_FEE;
            decimal thevat = Convert.ToDecimal(string.Format("{0:0.00}", computedVat));    //ensure it does not go beyond 2 decimal places   
            t.vat = thevat;
        }
        else if (t.Appid == 99) //zdvance
        {
            decimal ZDVANCE_FEE = appSettings.ZDVANCE_FEE;
            //compute the VAT (5% of NIP fee) 
            decimal computedVat = decimal.Parse("0.05") * ZDVANCE_FEE;
            //assign the new fee for fee and vat
            t.feecharge = ZDVANCE_FEE;
            decimal thevat = Convert.ToDecimal(string.Format("{0:0.00}", computedVat));//ensure it does not go beyond 2 decimal places
            t.vat = thevat;
        }
        else if (t.Appid == 1112) //Kudi.ai
        {
            decimal KUDI_FEE = appSettings.KUDI_FEE;
            //compute the VAT (5% of NIP fee) 
            decimal computedVat = decimal.Parse("0.05") * KUDI_FEE;
            //assign the new fee for fee and vat
            t.feecharge = KUDI_FEE;
            decimal thevat = Convert.ToDecimal(string.Format("{0:0.00}", computedVat)); //ensure it does not go beyond 2 decimal places
            t.vat = thevat;
        }

        XMLGenerator xg = new XMLGenerator(t.tellerID);
        xg.startDebit();

        //fee Account table to know what to debit
        //DataTable dtFeeTable = GetFreeFeeAccount(t.inCust.sub_acct_code).Tables[0];
        FreeFeeAccount? dtFeeTable = await transactionDetailsRepository.GetFreeFeeAccount(t.inCust.sub_acct_code);
        this.outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

        if (dtFeeTable != null && !string.IsNullOrEmpty(dtFeeTable?.feeAccount) && !string.IsNullOrEmpty(dtFeeTable?.VatAccount))
        {
            xg.addAccount(t.inCust.bra_code, t.inCust.cus_num, t.inCust.cur_code, t.inCust.led_code, t.inCust.sub_acct_code, t.Amount.ToString(), expl_code, xremark1);
            xg.addAccount(t.inCust.bra_code, t.inCust.cus_num, t.inCust.cur_code, t.inCust.led_code, dtFeeTable.feeAccount, t.feecharge.ToString(), Fee_expl_code, xremark2);
            xg.addAccount(t.inCust.bra_code, t.inCust.cus_num, t.inCust.cur_code, t.inCust.led_code, dtFeeTable.VatAccount, t.vat.ToString(), Fee_expl_code, xremark3);
            xg.endDebit();
            xg.startCredit();
            xg.addAccount(TSS_bra_code, TSS_cus_num, TSS_cur_code, TSS_led_code, TSS_sub_acct_code, t.Amount.ToString(), expl_code, xremark1);
            xg.addAccount(FEE_bra_code, FEE_cus_num, FEE_cur_code, FEE_led_code, FEE_sub_acct_code, t.feecharge.ToString(), Fee_expl_code, xremark2);
            xg.addAccount(t.VAT_bra_code, t.VAT_cus_num, t.VAT_cur_code, t.VAT_led_code, t.VAT_sub_acct_code, t.vat.ToString(), Fee_expl_code, xremark3);
            xg.endCredit();


        }
        else
        {
            xg.addAccount(t.inCust.bra_code, t.inCust.cus_num, t.inCust.cur_code, t.inCust.led_code, t.inCust.sub_acct_code, t.Amount.ToString(), expl_code, xremark1);
            xg.addAccount(t.inCust.bra_code, t.inCust.cus_num, t.inCust.cur_code, t.inCust.led_code, t.inCust.sub_acct_code, t.feecharge.ToString(), Fee_expl_code, xremark2);
            xg.addAccount(t.inCust.bra_code, t.inCust.cus_num, t.inCust.cur_code, t.inCust.led_code, t.inCust.sub_acct_code, t.vat.ToString(), Fee_expl_code, xremark3);
            xg.endDebit();
            xg.startCredit();
            xg.addAccount(TSS_bra_code, TSS_cus_num, TSS_cur_code, TSS_led_code, TSS_sub_acct_code, t.Amount.ToString(), expl_code, xremark1);
            xg.addAccount(FEE_bra_code, FEE_cus_num, FEE_cur_code, FEE_led_code, FEE_sub_acct_code, t.feecharge.ToString(), Fee_expl_code, xremark2);
            xg.addAccount(t.VAT_bra_code, t.VAT_cus_num, t.VAT_cur_code, t.VAT_led_code, t.VAT_sub_acct_code, t.vat.ToString(), Fee_expl_code, xremark3);
            xg.endCredit();

            //log.Info($"destination account: fee account:{FEE_sub_acct_code}, vat account: {t.VAT_sub_acct_code}, principal account: {TSS_sub_acct_code}");

            //log.Info($"customer account: fee account:{t.inCust.sub_acct_code}, vat account: {t.inCust.sub_acct_code}, principal account: {t.inCust.sub_acct_code}");
        }

        xg.closeXML();
        //new ErrorLog("Request to vTeller " + xg.req.ToString() + " for " + t.sessionid);
        var outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        try
        {
            outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.APIMethod = $"VTeller.nfpSoapClient.NIBBSAsync";
            outboundLog.RequestDetails = $"{xg.req}";
            using (VTeller.nfpSoapClient vs = new VTeller.nfpSoapClient(VTeller.nfpSoapClient.EndpointConfiguration.nfpSoap, appSettings.VTellerSoapService))
            {
                var vTellerResp = await vs.NIBBSAsync(xg.req, t.ID.ToString(), t.SessionID);
                xg.resp = vTellerResp.Body.NIBBSResult;
            }

            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            
            //xg.resp = vs.NIBBS(xg.req, "NIBSS Transfer from Sterling " + t.Refid);240000

            xg.parseResponse();

            ////collect returened values
            RespCreditedamt = xg.credits[0]["amount"].InnerText;
            vTellerResponse.Respreturnedcode1 = xg.debits[0]["return_status"].InnerText;

            Respreturnedcode2 = xg.debits[1]["return_status"].InnerText;

            vTellerResponse.error_text = xg.debits[0]["err_text"].InnerText;

            vTellerResponse.Prin_Rsp = xg.debits[0].ChildNodes[7].InnerText;
            vTellerResponse.Fee_Rsp = xg.debits[1].ChildNodes[7].InnerText;
            vTellerResponse.Vat_Rsp = xg.debits[2].ChildNodes[7].InnerText;

            // Prin_Rsp = xg.debits[0].ChildNodes[7].InnerText;
            // Fee_Rsp = xg.debits[1].ChildNodes[7].InnerText;
            // Vat_Rsp = xg.debits[2].ChildNodes[7].InnerText;

            StringBuilder resp = new StringBuilder();
            resp.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            resp.Append("<intTrnxResp>");
            resp.Append("<totaldebit>" + xg.credits[0]["amount"].InnerText + "</totaldebit>");
            resp.Append("<remark>" + xremark1 + "</remark>");
            // resp.Append("<remark>" + t.paymentRef + "</remark>");
            resp.Append("<principal>");
            resp.Append("<acc_num>");
            resp.Append(t.inCust.bra_code + TSS_cus_num + TSS_cur_code + TSS_led_code + TSS_sub_acct_code);
            resp.Append("</acc_num>");
            resp.Append("<tra_seq>" + xg.debits[0]["tra_seq"].InnerText + "</tra_seq>");
            resp.Append("<responseCode>" + xg.debits[0]["return_status"].InnerText + "</responseCode>");
            resp.Append("<responseText>" + xg.debits[0]["err_text"].InnerText + "</responseText>");
            resp.Append("</principal>");
            resp.Append("<fee>");
            resp.Append("<acc_num>");
            resp.Append(t.inCust.bra_code + FEE_cus_num + FEE_cur_code + FEE_led_code + FEE_sub_acct_code);
            resp.Append("</acc_num>");
            resp.Append("<tra_seq>" + xg.debits[1]["tra_seq"].InnerText + "</tra_seq>");
            resp.Append("<responseCode>" + xg.debits[1]["return_status"].InnerText + "</responseCode>");
            resp.Append("<responseText>" + xg.debits[1]["err_text"].InnerText + "</responseText>");
            resp.Append("</fee>");
            resp.Append("</intTrnxResp>");
            ResponseMsg = resp.ToString();

            outboundLog.ResponseDetails = $"{ResponseMsg}";
            
            //log.Info("Response from vTeller " + ResponseMsg + " for " + t.sessionid);
            
        }
        catch (Exception ex)
        {
            var rawRequest = JsonConvert.SerializeObject(t);
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
                "\r\n" + $@"RawRequest {rawRequest} Exception Details: {ex.Message} {ex.StackTrace}";
            //log.Error("Unable to debit customer account " + " The error " + ex + " " + t.inCust.bra_code + t.inCust.cus_num + t.inCust.cur_code + t.inCust.led_code + t.inCust.sub_acct_code + " for amount " + t.amount.ToString() + " and fee charge " + t.feecharge.ToString() + ex);
            vTellerResponse.Respreturnedcode1 = "1x";
            
        }
        this.outboundLogs.Add(outboundLog);
        
        return vTellerResponse;
    }
}