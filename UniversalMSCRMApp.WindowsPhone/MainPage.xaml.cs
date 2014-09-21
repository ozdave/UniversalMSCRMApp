//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//----------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net;
using System.Net.Http;
using System.Globalization;
using System.Net.Http.Headers;
using Windows.Data.Json;
using System.Linq;

using UniversalMSCRMApp.Models;
using UniversalMSCRMApp.ViewModels;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace UniversalMSCRMApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// The page implements IWebAuthenticationContinuable, as it is necessary for pages containing actions that can trigger authentication
    /// </summary>
    public sealed partial class MainPage : Page, IWebAuthenticationContinuable
    {
#region init

        //
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The Tenant is the name of the Azure AD tenant in which this application is registered.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Authority is the sign-in URL of the tenant.
        //

        //const string aadInstance = "https://login.windows.net/{0}";
        //const string tenant = "[Enter tenant name, e.g. contoso.onmicrosoft.com]";
        //const string clientId = "[Enter client ID as obtained from Azure Portal, e.g. 82692da5-a86f-44c9-9d53-2f88d52b478b]";

        //static string _authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

        private const string _clientID = "42d89e82-1a4b-4ab7-8593-06dd42584ef9";
        public const string CrmServiceUrl = "https://ip1409.crm.dynamics.com";   


        private HttpClient _httpClient = new HttpClient();
        private AuthenticationContext _authContext = null;
        private Uri _redirectURI = null;

        private AccountsViewModel _accountsVM;

#endregion
        public MainPage()
        {
            this.InitializeComponent();
            SoapUtils.ServiceUrl = CrmServiceUrl;
            _accountsVM = new AccountsViewModel();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            // Every Windows Store application has a unique URI.
            // Windows ensures that only this application will receive messages sent to this URI.
            // ADAL uses this URI as the application's redirect URI to receive OAuth responses.
            // 
            // To determine this application's redirect URI, which is necessary when registering the app
            //      in AAD, set a breakpoint on the next line, run the app, and copy the string value of the URI.
            //      This is the only purposes of this line of code, it has no functional purpose in the application.
            //
            // store app:  "ms-app://s-1-15-2-1797451609-2481545285-2169326427-3565185340-866651618-3840574542-3393684411/"
            // phone app:  {ms-app://s-1-15-2-2671709060-4147598678-3876637945-4029922067-1409672279-882262885-2759017160/}

            _redirectURI = Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri();

            // Dyamics CRM Online OAuth URL.
            Uri serviceUri = new Uri(CrmServiceUrl + "/XRMServices/2011/Organization.svc/web?SdkClientVersion=6.1.0000.0000");
            string _oauthUrl = SoapUtils.GetOAuthAuthority(serviceUri);
            //string _oauthUrl = "https://login.windows.net/8ffacab8-e7fe-4f54-b08f-9da391ab5006/oauth2/authorize";

            // ADAL for Windows Phone 8.1 builds AuthenticationContext instances through a factory, which performs authority validation at creation time
            _authContext = AuthenticationContext.CreateAsync(_oauthUrl).GetResults();
        }


        #region IWebAuthenticationContinuable implementation
        
        // This method is automatically invoked when the application is reactivated after an authentication interaction throuhg WebAuthenticationBroker.        
        public async void ContinueWebAuthentication(WebAuthenticationBrokerContinuationEventArgs args)
        {
            // pass the authentication interaction results to ADAL, which will conclude the token acquisition operation and invoke the callback specified in AcquireTokenAndContinue.
            await _authContext.ContinueAcquireTokenAsync(args);
        }
        #endregion
        


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
           
        }

        #region Callbacks

        /// <summary>
        /// Retrieve the user's account list. This is called from RefreshAppBarButton_Click, either directly if a authentication token
        /// has been obtained, or indirectly after authentication by the WebAuthenticationBroker
        /// </summary>
        /// <param name="result"></param>
        public async void GetAccountList(AuthenticationResult result) {

            if (result.Status == AuthenticationStatus.Success) {
                await _accountsVM.LoadAccountsData(result.AccessToken);
                AccountList.ItemsSource = from account in _accountsVM.Accounts
                                          select new {
                                              Title = account.Name
                                          };
            }
            else {
                MessageDialog dialog = new MessageDialog(string.Format("If the error continues, please contact your administrator.\n\nError: {0}\n\nError Description:\n\n{1}", result.Error, result.ErrorDescription), "Sorry, an error occurred while signing you in.");
                await dialog.ShowAsync();
            }
        }

        #endregion

        #region AppBar buttons


        // fetch the user's Account list from the service. If no tokens are present in the cache, trigger the authentication experience before performing the call
        private async void RefreshAppBarButton_Click(object sender, RoutedEventArgs e) {
            // Try to get a token without triggering any user prompt. 
            // ADAL will check whether the requested token is in the cache or can be obtained without user itneraction (e.g. via a refresh token).
            AuthenticationResult result = await _authContext.AcquireTokenSilentAsync(CrmServiceUrl, _clientID);
            if (result != null && result.Status == AuthenticationStatus.Success) {
                // A token was successfully retrieved. Get the To Do list for the current user
                GetAccountList(result);
            }
            else {
                // Acquiring a token without user interaction was not possible. 
                // Trigger an authentication experience and specify that once a token has been obtained the GetTodoList method should be called
                _authContext.AcquireTokenAndContinue(CrmServiceUrl, _clientID, _redirectURI, GetAccountList);
            }
        }

        // clear the token cache
        private void RemoveAppBarButton_Click(object sender, RoutedEventArgs e)
        {
             // Clear session state from the token cache.
            _authContext.TokenCache.Clear();

            // Reset UI elements
            AccountList.ItemsSource = null;
            //TodoText.Text = "";
        }

        #endregion

    
    }


}
