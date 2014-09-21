// =====================================================================
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
// =====================================================================

//<snippetModernSoapApp>
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.Security.Authentication.Web;
using System.Net;
using System.Threading;
using System.IO;
using System.Text;

namespace UniversalMSCRMApp
{
    /// <summary>
    /// Manages authentication with the organization web service.
    /// </summary>
   public static class CurrentEnvironment
   {
       # region Class Level Members

       private static AuthenticationContext _authenticationContext;

       // TODO Set these string values as approppriate for your app registration and organization.
       // For more information, see the SDK topic "Walkthrough: Register an app with Active Directory".
       private const string _clientID = "42d89e82-1a4b-4ab7-8593-06dd42584ef9";
       public const string CrmServiceUrl = "https://ip1409.crm.dynamics.com";       
     
       # endregion

       // <summary>
       /// Perform any required app initialization.
       /// This is where authentication with Active Directory is performed.
       /// Returns the token from the Azure Active Directory authentication
       public static async Task<string> Initialize()
       {
           SoapUtils.ServiceUrl = CrmServiceUrl;

           Uri serviceUrl = new System.Uri(CrmServiceUrl + "/XRMServices/2011/Organization.svc/web?SdkClientVersion=6.1.0000.0000");

           // Dyamics CRM Online OAuth URL.
           string _oauthUrl = SoapUtils.GetOAuthAuthority(serviceUrl);

           // Obtain the redirect URL for the app. This is only needed for app registration.  Wndows.Security.Authentication
           Uri redirectUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();

           // Obtain an authentication token to access the web service. Windows Azue Active Directory
           _authenticationContext = new AuthenticationContext(_oauthUrl, false);

           AuthenticationResult result = await _authenticationContext.AcquireTokenAsync(CrmServiceUrl, _clientID, redirectUri);

           // Verify that an access token was successfully acquired.
           if (AuthenticationStatus.Success != result.Status) {
               if (result.Error == "authentication_failed") {
                   // Clear the token cache and try again.
                   _authenticationContext.TokenCache.Clear();
                   _authenticationContext = new AuthenticationContext(_oauthUrl, false);
                   result = await _authenticationContext.AcquireTokenAsync(CrmServiceUrl, _clientID, redirectUri);
               }
               else
               {
                   DisplayErrorWhenAcquireTokenFails(result);
               }
           }
           return result.AccessToken;
       }



        /// <summary>
        /// Display an error message to the user.
        /// </summary>
        /// <param name="result">The authentication result returned from AcquireTokenAsync().</param>
        private static async void DisplayErrorWhenAcquireTokenFails(AuthenticationResult result)
        {
            MessageDialog dialog;

            switch (result.Error)
            {
                case "authentication_canceled":
                    // User cancelled, so no need to display a message.
                    break;
                case "temporarily_unavailable":
                case "server_error":
                    dialog = new MessageDialog("Please retry the operation. If the error continues, please contact your administrator.",
                        "Sorry, an error has occurred.");
                    await dialog.ShowAsync();
                    break;
                default:
                    // An error occurred when acquiring a token so show the error description in a MessageDialog.
                    dialog = new MessageDialog(string.Format(
                        "If the error continues, please contact your administrator.\n\nError: {0}\n\nError Description:\n\n{1}",
                        result.Error, result.ErrorDescription), "Sorry, an error has occurred.");
                    await dialog.ShowAsync();
                    break;
            }
        }
    }
}
//</snippetModernSoapApp>