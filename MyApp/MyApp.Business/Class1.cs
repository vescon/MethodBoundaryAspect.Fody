using System.Diagnostics;

namespace MyApp.Business
{
    public class BL
    {
        [TransactionScope]
        public void ClassService()
        {
            var da = new DataAccess.DataAccess();
            da.ServiceCall();
            
            Debug.WriteLine("Do some database stuff isolated in surrounding transaction");
        }
    }
}
