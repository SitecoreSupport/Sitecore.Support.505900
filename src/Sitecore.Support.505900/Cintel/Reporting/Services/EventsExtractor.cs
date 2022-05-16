using System.Collections.Generic;
using System.Linq;
using Sitecore.Cintel.Reporting.DTOs;
using Sitecore.Cintel.Reporting.Services.DataResolvers;
using Sitecore.Cintel.Reporting.Services.Filters;
using Sitecore.Cintel.Reporting.Services;
using Sitecore.Diagnostics;
using Sitecore.XConnect;
using Sitecore.XConnect.Collection.Model;

namespace Sitecore.Support.Cintel.Reporting.Services
{
    public class EventsExtractor : IEventsExtractor
    {
        public EventsExtractor(IReadOnlyList<IEntityFilter<Event>> filter, IReadOnlyList<IDataResolver<EventDTO>> dataResolvers)
        {
            Filters = filter;
            DataResolvers = dataResolvers;
        }

        private IReadOnlyList<IEntityFilter<Event>> Filters { get; }
        private IReadOnlyList<IDataResolver<EventDTO>> DataResolvers { get; }

        public List<EventDTO> ExtractFromInteraction(Interaction interaction)
        {
            Assert.ArgumentNotNull(interaction, nameof(interaction));

            if (!interaction.Events.Any())
                return null;

            var result = new List<EventDTO>(interaction.Events.Count);
            var ipInfo = interaction.IpInfo();

            foreach (var @event in interaction.Events)
            {
                if (!IsMatchingFiltersCriteria(@event))
                    continue;

                var eventDto = new EventDTO()
                {
                    VisitId = interaction.Id,
                    SiteName = interaction.WebVisit()?.SiteName,
                    ContactId = interaction.Contact.Id.GetValueOrDefault(),
                    EventDateTime = @event.Timestamp,
                    EventId = @event.Id,
                    ImageUrl = string.Format("/sitecore/api/ao/v1/analytics/pageevents/{0}/image", @event.DefinitionId),
                    RelatedInteraction = interaction,
                    RelatedEvent = @event,
                    Url = GetUrl(@event, interaction),
                    LatestVisitRegionDisplayName = ipInfo?.Region,
                    LatestVisitCountryDisplayName = ipInfo?.Country,
                    LatestVisitBusinessName = ipInfo?.BusinessName,
                    LatestVisitCityDisplayName = ipInfo?.City
                };                

                ResolveDataForEvent(eventDto);
                result.Add(eventDto);
            }

            return result;
        }

        private static string GetUrl(Event item, Interaction interaction)
        {
            string url = null;
            if (item.GetType() == typeof(PageViewEvent))
            {
                url = ((PageViewEvent)item).Url;
            }
            if (item.ParentEventId.HasValue)
            {
                var relatedView = interaction.Events.OfType<PageViewEvent>().FirstOrDefault(o => o.Id == item.ParentEventId.Value);
                url = relatedView?.Url;
            }

            return url ?? string.Empty;
        }

        private bool IsMatchingFiltersCriteria(Event @event)
        {
            if (Filters == null || !Filters.Any())
                return true;

            foreach (var filter in Filters)
            {
                if (!filter.IsMatchCriteria(@event))
                    return false;
            }

            return true;
        }

        private void ResolveDataForEvent(EventDTO @event)
        {
            if (DataResolvers == null || !DataResolvers.Any())
                return;

            foreach (var resolver in DataResolvers)
                resolver.Resolve(@event);
        }
    }
}