﻿namespace GF.UCenter.Web
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Web.Http;
    using Common;
    using Common.Portable;
    using Common.Settings;
    using CouchBase;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     UCenter payment api controller
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [RoutePrefix("api/payment")]
    public class PaymentApiController : ApiControllerBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PaymentApiController" /> class.
        /// </summary>
        /// <param name="db">The couch base context.</param>
        /// <param name="settings">The UCenter settings.</param>
        [ImportingConstructor]
        public PaymentApiController(CouchBaseContext db, Settings settings)
            : base(db)
        {
        }

        [Route("charge")]
        public IHttpActionResult Charge([FromBody] ChargeInfo info)
        {
            Logger.Info($"AppServer支付请求\nAppId={info.AppId}\nAccountId={info.AccountId}");

            try
            {
                // TODO: Replace with live key
                Pingpp.Pingpp.SetApiKey("sk_test_zXnD8KKOyfn1vDuj9SG8ibfT");
                // TODO: Fix hard code path
                Pingpp.Pingpp.SetPrivateKeyPath(@"C:\git\UCenter\src\GF.UCenter.Web\App_Data\rsa_public_key.pem");

                var appId = "app_H4yDu5COi1O4SWvz";
                var r = new Random();
                string orderNoPostfix = r.Next(0, 1000000).ToString("D6");
                string orderNo = $"{DateTime.Now.ToString("yyyyMMddHHmmssffff")}_{orderNoPostfix}";
                var amount = info.Amount;
                var channel = "alipay";
                var currency = "cny";

                //交易请求参数，这里只列出必填参数，可选参数请参考 https://pingxx.com/document/api#api-c-new
                var chParams = new Dictionary<string, object>
                {
                    {"order_no", orderNo},
                    {"amount", amount},
                    {"channel", "wx"},
                    {"currency", "cny"},
                    {"subject", "Your Subject"},
                    {"body", "Your Body"},
                    {"client_ip", "127.0.0.1"},
                    {"app", new Dictionary<string, string> {{"id", appId}}}
                };
                
                var charge = Pingpp.Models.Charge.Create(chParams);

                return CreateSuccessResult(charge);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建Charge失败");
                throw new UCenterException(UCenterErrorCode.PaymentCreateChargeFailed, ex.Message);
            }
        }

        [HttpPost]
        [Route("webhook")]
        public IHttpActionResult WebHook()
        {
            Logger.Info("UCenter接收到ping++回调消息");

            //获取 post 的 event对象
            var inputData = Request.Content.ReadAsStringAsync().Result;
            Logger.Info("消息内容\n" + inputData);

            //获取 header 中的签名
            IEnumerable<string> headerValues;
            string sig = string.Empty;
            if (Request.Headers.TryGetValues("x-pingplusplus-signature", out headerValues))
            {
                sig = headerValues.FirstOrDefault();
            }

            //公钥路径（请检查你的公钥 .pem 文件存放路径）
            var path = @"~/App_Data/rsa_public_key.pem";

            //验证签名
            var result = VerifySignedHash(inputData, sig, path);

            var jObject = JObject.Parse(inputData);
            var type = jObject.SelectToken("type");

            if (type.ToString() == "charge.succeeded" || type.ToString() == "refund.succeeded")
            {
                // TODO what you need do
                //Response.StatusCode = 200;
            }

            return CreateSuccessResult("Success received order info");
        }

        public static string VerifySignedHash(string str_DataToVerify, string str_SignedData,
            string str_publicKeyFilePath)
        {
            byte[] SignedData = Convert.FromBase64String(str_SignedData);

            var ByteConverter = new UTF8Encoding();
            byte[] DataToVerify = ByteConverter.GetBytes(str_DataToVerify);
            try
            {
                string sPublicKeyPEM = File.ReadAllText(str_publicKeyFilePath);
                var rsa = new RSACryptoServiceProvider();

                rsa.PersistKeyInCsp = false;
                rsa.LoadPublicKeyPEM(sPublicKeyPEM);

                if (rsa.VerifyData(DataToVerify, "SHA256", SignedData))
                {
                    return "verify success";
                }
                return "verify fail";
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return "verify error";
            }
        }
    }
}