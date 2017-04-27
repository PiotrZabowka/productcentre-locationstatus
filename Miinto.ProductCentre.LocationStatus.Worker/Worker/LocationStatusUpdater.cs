using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Miinto.ProductCentre.LocationStatus.Worker.Messaging;

namespace Miinto.ProductCentre.LocationStatus.Worker
{
    public class LocationStatusUpdater
    {
        private readonly LocationStatusRepository locationStatusRepository;
        public LocationStatusUpdater()
        {
            locationStatusRepository = new LocationStatusRepository();
        }
        public void HandleEvent(IList<ProductCreationRequestProcessed> aggregatedEvents)
        {
            var locationNotChangedMessages = aggregatedEvents.Where(e => e.LocationStatusNotChanged).Select(e => e);
            var pcrProcessedMessages = aggregatedEvents.Where(e => !e.LocationStatusNotChanged).Select(e => e);

            HandleLocationNotChangedMessages(locationNotChangedMessages);
            HandlePcrProcessedMessages(pcrProcessedMessages);

        }

        private void HandlePcrProcessedMessages(IEnumerable<ProductCreationRequestProcessed> pcrProcessedMessages)
        {
            var updateRequests = pcrProcessedMessages.ToLookup(ev => new { ev.LocationId, ev.SessionId })
                  .Select(loc => new UpdateLocationStatusRequest
                  {
                      LocationId = loc.Key.LocationId,
                      SessionId = loc.Key.SessionId,
                      SessionDate = loc.First().SessionDate,
                      ProcessedProducts = loc.Count(),
                      ValidProcessedProducts = loc.Count(m => m.WithSuccess)
                  }).ToList();

            if (!updateRequests.Any())
                return;

            locationStatusRepository.UpdateMultipleLocationStatuses(updateRequests).Wait();
        }

        private void HandleLocationNotChangedMessages(IEnumerable<ProductCreationRequestProcessed> locationNotChangedMessages)
        {
            var updateRequests = locationNotChangedMessages.Select(ev => new UpdateLocationStatusSessionRequest
            {
                SessionDate = ev.SessionDate,
                SessionId = ev.SessionId
            }).ToList();

            if (!updateRequests.Any())
                return;

            locationStatusRepository.UpdateMultipleLocationStatusSessionDate(updateRequests).Wait();
        }
    }
}
