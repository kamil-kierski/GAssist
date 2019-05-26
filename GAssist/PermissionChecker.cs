using System;
using Tizen.Security;

namespace GAssist
{
    internal class PermissionChecker
    {
        public const string recorderPermission = "http://tizen.org/privilege/recorder";
        public const string mediaStoragePermission = "http://tizen.org/privilege/mediastorage";

        public static void CheckAndRequestPermission(string permission)
        {
            SetupPPMHandler(permission);
            try
            {
                var result = PrivacyPrivilegeManager.CheckPermission(permission);
                switch (result)
                {
                    case CheckResult.Allow:
                        /// Update UI and start accessing protected functionality
                        break;

                    case CheckResult.Deny:
                        PrivacyPrivilegeManager.RequestPermission(permission);
                        break;

                    case CheckResult.Ask:
                        PrivacyPrivilegeManager.RequestPermission(permission);
                        break;
                }
            }
            catch (Exception)
            {
                /// Handle exception
            }
        }

        private static void SetupPPMHandler(string privilege)
        {
            PrivacyPrivilegeManager.ResponseContext context = null;
            if (PrivacyPrivilegeManager.GetResponseContext(privilege).TryGetTarget(out context))
                context.ResponseFetched += PPMResponseHandler;
        }

        private static void PPMResponseHandler(object sender, RequestResponseEventArgs e)
        {
            if (e.cause == CallCause.Error)
                /// Handle errors
                return;

            switch (e.result)
            {
                case RequestResult.AllowForever:
                    /// Update UI and start accessing protected functionality
                    break;

                case RequestResult.DenyForever:
                    /// Show a message and terminate the application
                    break;

                case RequestResult.DenyOnce:
                    /// Show a message with explanation
                    break;
            }
        }
    }
}