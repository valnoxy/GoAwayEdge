namespace GoAwayEdge.Common
{
    public class Removal
    {
        /// <summary>
        /// Removes Microsoft Edge completely from the system.
        /// </summary>
        /// <returns>
        ///     Result of the removal as boolean.
        /// </returns>
		public static bool RemoveMsEdge()
        {
            //
            // Ok, this is very wip. Current plan:
            //
            //  1. Remove Edge via edge setup (setup.exe --uninstall --system-level --verbose-logging --force-uninstall)
            //  2. Prevent Edge from reinstalling
            //  3. Recreate the URI protocol
            // 


            return false;
        }
    }
}
