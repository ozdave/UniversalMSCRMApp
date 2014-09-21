// =====================================================================
//
//  This file is part of the Microsoft Dynamics CRM SDK Code Samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or online documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
//
// =====================================================================

//<snippetModernSoapApp2>
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalMSCRMApp
{
    public static class SoapUtils
    {



        /// <summary>
        /// Get the DefaultRequestTimeout from the server.
        /// </summary>
        public static TimeSpan DefaultRequestTimeout { get; set; }


        /// <summary>
        /// Set the service url
        /// </summary>
        public static string ServiceUrl { get; set; }



        /// <summary>
        /// Method to get authority URL from organization’s SOAP endpoint URL using Microsoft Azure Active Directory Authentication Library (ADAL), 
        /// </summary>
        /// <param name="result">The Authority Url returned from HttpWebResponse.</param>
        public static string GetOAuthAuthority(System.Uri serviceUri) {
            // Use AuthenticationParameters to send a request to the organization's endpoint and
            // receive tenant information in the 401 challenge. 
            Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationParameters parameters = null;
            HttpWebResponse response = null;
            try {
                // Create a web request where the authorization header contains the word "Bearer".
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(serviceUri);
                httpWebRequest.Headers[HttpRequestHeader.Authorization.ToString()] = "Bearer";

                // The response is to be encoded.
                httpWebRequest.Accept = "application/x-www-form-urlencoded";
                response = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (WebException ex) {
                response = (HttpWebResponse)ex.Response;

                // A good response was returned. Extract any parameters from the response.
                // The response should contain an authorization_uri parameter.
                parameters = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationParameters.
                    CreateFromResponseAuthenticateHeader((response.Headers)["WWW-Authenticate"]);
            }
            finally {
                if (response != null)
                    response.Dispose();
            }
            // Return the authority URL.
            return parameters.Authority;
        }



        /// <summary>
        /// Extension method on WebRequest to get the response
        /// Returns a response from an Internet resource. 
        /// </summary>       
        public static WebResponse GetResponse(this WebRequest request) {
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);
            IAsyncResult asyncResult = request.BeginGetResponse(r => autoResetEvent.Set(), null);

            // Wait until the call is finished
            autoResetEvent.WaitOne(DefaultRequestTimeout);
            return request.EndGetResponse(asyncResult);
        }




        /// <summary>
        /// Retrieve entity record data from the organization web service. 
        /// </summary>
        /// <param name="accessToken">The web service authentication access token.</param>
        /// <param name="entity">The target entity for which the data should be retreived.</param>
        /// <param name="Columns">The entity attributes to retrieve.</param>
        /// <returns>Response from the web service.</returns>
        /// <remarks>Builds a SOAP HTTP request using passed parameters and sends the request to the server.</remarks>
        public static async Task<string> RetrieveMultiple(string accessToken, string entity, string[] Columns)
        {
            // Build a list of entity attributes to retrieve as a string.
            string columnsSet = string.Empty;
            foreach (string Column in Columns)
            {
                columnsSet += "<b:string>" + Column + "</b:string>";
            }

            // Default SOAP envelope string. This XML code was obtained using the SOAPLogger tool.
            string xmlSOAP =
             @"<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'>
                <s:Body>
                  <RetrieveMultiple xmlns='http://schemas.microsoft.com/xrm/2011/Contracts/Services' xmlns:i='http://www.w3.org/2001/XMLSchema-instance'>
                    <query i:type='a:QueryExpression' xmlns:a='http://schemas.microsoft.com/xrm/2011/Contracts'><a:ColumnSet>
                    <a:AllColumns>false</a:AllColumns><a:Columns xmlns:b='http://schemas.microsoft.com/2003/10/Serialization/Arrays'>" + columnsSet +
                   @"</a:Columns></a:ColumnSet><a:Criteria><a:Conditions /><a:FilterOperator>And</a:FilterOperator><a:Filters /></a:Criteria>
                    <a:Distinct>false</a:Distinct><a:EntityName>" + entity + @"</a:EntityName><a:LinkEntities /><a:Orders />
                    <a:PageInfo><a:Count>0</a:Count><a:PageNumber>0</a:PageNumber><a:PagingCookie i:nil='true' />
                    <a:ReturnTotalRecordCount>false</a:ReturnTotalRecordCount>
                    </a:PageInfo><a:NoLock>false</a:NoLock></query>
                  </RetrieveMultiple>
                </s:Body>
              </s:Envelope>";

            // The URL for the SOAP endpoint of the organization web service.
            string url = ServiceUrl  + "/XRMServices/2011/Organization.svc/web";

            // Use the RetrieveMultiple CRM message as the SOAP action.
            string SOAPAction = "http://schemas.microsoft.com/xrm/2011/Contracts/Services/IOrganizationService/RetrieveMultiple";

            // Create a new HTTP request.
            HttpClient httpClient = new HttpClient();

            // Set the HTTP authorization header using the access token.
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Finish setting up the HTTP request.
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("SOAPAction", SOAPAction);
            req.Method = HttpMethod.Post;
            req.Content = new StringContent(xmlSOAP);
            req.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml; charset=utf-8");

            // Send the request asychronously and wait for the response.
            HttpResponseMessage response;
            response = await httpClient.SendAsync(req);
            var responseBodyAsText = await response.Content.ReadAsStringAsync();

            return responseBodyAsText;
        }
    }
}
//</snippetModernSoapApp2>
