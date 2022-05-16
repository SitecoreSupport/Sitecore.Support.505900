using System.Collections.Generic;
using System.Data;
using Sitecore.Cintel.Reporting;
using Sitecore.Cintel.Reporting.DTOs;
using Sitecore.Cintel.Reporting.Services;
using Sitecore.Cintel.Reporting.Services.DataProcessors;
using Sitecore.Cintel.Reporting.Services.DataResolvers;
using Sitecore.Cintel.Reporting.Services.Filters;
using Sitecore.Cintel.Reporting.Services.Mappers;
using Sitecore.Cintel.Reporting.Services.Repositories;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.Marketing.Definitions;
using Sitecore.Pipelines;
using Sitecore.XConnect;
using Sitecore.XConnect.Client.Configuration;
using EventsExtractor = Sitecore.Support.Cintel.Reporting.Services.EventsExtractor;

namespace Sitecore.Support.Cintel.Reporting
{
    public static class ViewFactory
    {
        #region Private Variables
        #endregion

        #region Constructor(s)
        #endregion

        /// <summary>
        /// Returns the pipeline for the given <see cref="ViewParameters"/>
        /// </summary>
        /// <returns></returns>
        public static CorePipeline GetPipeline([NotNull]ViewParameters parameters, string groupName)
        {
            string pipelineName = parameters.ViewName;
            return GetPipeline(pipelineName, groupName);
        }

        public static CorePipeline GetPipeline(string pipelineName, string groupName)
        {
            var pipeline = CorePipelineFactory.GetPipeline(pipelineName, groupName);
            Assert.IsNotNull(pipeline, "No pipeline was found for View [{0}]", pipelineName);
            return pipeline;
        }

        public static IEventsReportService GetEventsReportService()
        {
            return new EventsReportService(GetContactInteractionsRepository(),
                new EventsExtractor(new IEntityFilter<Event>[]
            {
                new EventsToBeShownInListFilter(ServiceLocator.ServiceProvider.GetDefinitionManagerFactory()),
            }, new IDataResolver<EventDTO>[] { new EventSubjectItemDataResolver(Context.Database), new EventTypeDataResolver(new SitecoreTranslatorWrapper()) }), null);
        }

        public static IEventsReportService GetLatestEventsReportService()
        {
            return new EventsReportService(GetContactInteractionsRepository(),
                new EventsExtractor(new IEntityFilter<Event>[]
                {
                    new LatestEventsFilter(ServiceLocator.ServiceProvider.GetDefinitionManagerFactory()),
                }, new IDataResolver<EventDTO>[] { new EventSubjectItemDataResolver(Context.Database), new EventTypeDataResolver(new SitecoreTranslatorWrapper()) }),
                new IDataProcessor<List<EventDTO>>[] { new LatestEventResultProcessor(new SitecoreTranslatorWrapper(), 10) });
        }

        public static IMapper<List<EventDTO>, DataTable> GetEventToDataTableMapper()
        {
            return new EventToDataTableMapper();
        }

        private static IContactInteractionsRepository GetContactInteractionsRepository()
        {
            return new ContactInteractionsRepository(new SitecoreXConnectClientFactory());
        }
    }
}