﻿using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using GameCloud.Database.Adapters;
using GameCloud.UCenter.Common.IP;
using GameCloud.UCenter.Common.Models.AppServer;
using GameCloud.UCenter.Common.Portable.Contracts;
using GameCloud.UCenter.Common.Portable.Exceptions;
using GameCloud.UCenter.Common.Portable.Models.AppClient;
using GameCloud.UCenter.Common.Settings;
using GameCloud.UCenter.Database;
using GameCloud.UCenter.Database.Entities;
using GameCloud.UCenter.Web.Common.Logger;
using GameCloud.UCenter.Web.Common.Storage;

namespace GameCloud.UCenter.Web.Api.ApiControllers
{
    /// <summary>
    /// UCenter account API controller
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class AppClientApiController : ApiControllerBase
    {
        private readonly StorageAccountContext storageContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppClientApiController" /> class.
        /// </summary>
        /// <param name="database">The database context.</param>
        /// <param name="settings">The UCenter settings.</param>
        /// <param name="storageContext">The storage account context.</param>
        [ImportingConstructor]
        public AppClientApiController(UCenterDatabaseContext database, Settings settings, StorageAccountContext storageContext)
            : base(database)
        {
            this.storageContext = storageContext;
        }

        /// <summary>
        /// Get client IP area.
        /// </summary>
        /// <param name="token">Indicating the cancellation token.</param>
        /// <returns>Async task.</returns>
        [HttpGet]
        [Route("api/appclient/ip")]
        public IHttpActionResult GetClientIpArea(CancellationToken token)
        {
            string ipAddress = IPHelper.GetClientIpAddress(Request);
            return this.CreateSuccessResult(ipAddress);
        }

        /// <summary>
        /// Create application configuration
        /// </summary>
        /// <param name="info">Indicating the application information.</param>
        /// <param name="token">Indicating the cancellation token.</param>
        /// <returns>Async task.</returns>
        [HttpPost]
        [Route("api/appclient/createconf")]
        public async Task<IHttpActionResult> CreateAppConfiguration([FromBody] AppConfigurationInfo info, CancellationToken token)
        {
            var appConfiguration = await this.Database.AppConfigurations.GetSingleAsync(info.AppId, token);

            if (appConfiguration == null)
            {
                appConfiguration = new AppConfigurationEntity
                {
                    Id = info.AppId,
                    Configuration = info.Configuration,
                };

                await this.Database.AppConfigurations.InsertAsync(appConfiguration, token);
            }
            else
            {
                await this.Database.AppConfigurations.UpsertAsync(appConfiguration, token);
            }

            var response = new AppConfigurationResponse
            {
                AppId = info.AppId,
                Configuration = info.Configuration
            };
            return this.CreateSuccessResult(response);
        }

        /// <summary>
        /// Get the APP configuration.
        /// </summary>
        /// <param name="appId">Indicating the App id.</param>
        /// <param name="token">Indicating the cancellation token.</param>
        /// <returns>Async task.</returns>
        [HttpPost]
        [Route("api/appclient/conf")]
        public async Task<IHttpActionResult> GetAppConfiguration([FromUri]string appId, CancellationToken token)
        {
            var app = await this.Database.Apps.GetSingleAsync(appId, token);
            if (app == null)
            {
                throw new UCenterException(UCenterErrorCode.AppNotExists);
            }

            var appConfiguration = await this.Database.AppConfigurations.GetSingleAsync(appId, token);

            var response = new AppConfigurationResponse
            {
                AppId = app.Id,
                Configuration = appConfiguration == null ? string.Empty : appConfiguration.Configuration
            };

            return this.CreateSuccessResult(response);
        }
    }
}