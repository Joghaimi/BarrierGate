using Models;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib
{
    public static class DBHundler
    {
        public static AppDbContext db = new AppDbContext();
        public static void AddGateActionsToDB(GateActions gateAction)
        {
            db.GateActions.Add(gateAction);
            db.SaveChanges();
        }
        public static List<GateActions> GetUnsentGateActions()
        {
            return db.GateActions.Where(gt => !gt.isSent).ToList();
        }

        public static void UpdateGateActionInDB(GateActions updatedAction)
        {
            var existingAction = db.GateActions.FirstOrDefault(a => a.Id == updatedAction.Id);

            if (existingAction != null)
            {
                existingAction.isSent = updatedAction.isSent;
                db.SaveChanges();
            }
        }

    }

}
