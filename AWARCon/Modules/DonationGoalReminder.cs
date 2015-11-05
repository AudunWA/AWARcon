using AWARCon.MySQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AWARCon.Modules
{
    class DonationGoalReminder
    {
        public const int MONTHLY_GOAL = 65;
        private Client _server;
        private Timer _timer;

        public DonationGoalReminder(Client server)
        {
            _server = server;
            if (!_server.IsOrigins)
                StartTimer();
        }

        private void StartTimer()
        {
            _timer = new Timer(1000 * 60 * 25); // 25 min
            _timer.Elapsed += TimerEvent;
            _timer.Start();
            //TimerEvent(null, null);
        }

        private void TimerEvent(object sender, ElapsedEventArgs e)
        {
            using (DatabaseClient dbClient = Program.DBManager.GetWebDBClient())
            {
                DateTime lastInvoice = DateTime.Now.Day > 14 ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 14) : new DateTime(DateTime.Now.Year, DateTime.Now.Month - 1, 14);
                string lastInvoiceStamp = lastInvoice.ToString("yyyy-MM-dd HH:mm:ss");
               dbClient.AddParameter("lastinvoice", lastInvoiceStamp);
               int currentSum = dbClient.ReadInt32("SELECT sum(amount) FROM yb_donations WHERE timestamp > @lastinvoice");

               if (MONTHLY_GOAL - currentSum <= 0)
                   _server.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Say, String.Format("-1 We have reached this month's donation goal ({0} euro)! Thank you everyone who donated!", MONTHLY_GOAL));
               else
                   _server.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Say, String.Format("-1 We need {0} more euro until {1} to pay the server bill ({2}/{3} euro raised so far)", MONTHLY_GOAL - currentSum, lastInvoice.AddMonths(1), currentSum, MONTHLY_GOAL));

               _server.GetBattlEyeClient().SendCommand(BattleNET.BattlEyeCommand.Say, "-1 More information about donations and the (hopefully) non-pay2win perks on awkack.org!");
            }
        }

    }
}
