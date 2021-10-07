namespace Sitecore.Support
{
    using System;
    using System.Linq;
    using Sitecore.Analytics;
    using Sitecore.Analytics.Data;
    using Sitecore.Analytics.Media;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Events;
    using Sitecore.Framework.Conditions;
    using Sitecore.Resources.Media;
    using Sitecore.Xdb.Configuration;

    [UsedImplicitly]
    public class MediaRequestEventHandler : Analytics.Media.MediaRequestEventHandler
    {
        /// <summary>
        /// Called when the media has request.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="args">
        /// The arguments.
        /// </param>
        [UsedImplicitly]
        public override void OnMediaRequest([NotNull] object sender, [CanBeNull] EventArgs args)
        {
            Condition.Requires(sender, nameof(sender)).IsNotNull();
            Condition.Requires(args, nameof(args)).IsNotNull();

            if (!XdbSettings.Tracking.Enabled)
            {
                return;
            }

            var site = Context.Site;
            if (site == null || !site.Tracking().EnableTracking)
            {
                return;
            }

            var request = Event.ExtractParameter(args, 0) as MediaRequest;
            if (request == null)
            {
                return;
            }

            var wrapper = new MediaRequestTrackingInformation(request);

            var item = wrapper.GetMediaItem();
            if (item == null)
            {
                return;
            }

            if (!wrapper.IsTrackedRequest())
            {
                return;
            }

            using (new ContextItemSwitcher(item))
            {
                try
                {
                    StartTracking();

                    var currentTracker = Tracker.Current;
                    Condition.Requires(currentTracker, nameof(Tracker)).IsNotNull("Tracker.Current is not initialized");
                    var session = currentTracker.Session;
                    Condition.Requires(session, nameof(Tracker)).IsNotNull("Tracker.Current.Session is not initialized");

                    var interaction = session.Interaction;

                    if (interaction != null)
                    {
                        var currentPage = interaction.CurrentPage;

                        if (currentPage != null)
                        {
                            var previousPage = interaction.PreviousPage;

                            if (previousPage != null)
                            {
                                if (currentPage.PageEvents.Any())
                                {
                                    var pageEvents = currentPage.PageEvents.Select(i => new PageEventData(i));

                                    previousPage.RegisterEvents(pageEvents);
                                }

                                currentPage.Cancel();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Media request analytics failed", ex, GetType());
                }
            }
        }
    }
}