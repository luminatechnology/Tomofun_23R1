using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PX.Objects.SO.SOOrderEntry;

namespace PX.Objects.SO
{
    #region Protected Access
    [PXProtectedAccess]
    public abstract class SOOrderEntry2_ProtectedExt : PXGraphExtension<SOOrderEntry>
    {
        [PXProtectedAccess(typeof(SOOrderEntry))]
        public abstract void SOLineValidateMinGrossProfit(PXCache sender, SOLine row, MinGrossProfitClass mgpc);

        [PXProtectedAccess(typeof(SOOrderEntry))]
        public abstract bool IsManualPriceFlagNeeded(PXCache sender, SOLine row);
    }

    #endregion

    public class SOOrderEntryExt2 : PXGraphExtension<SOOrderEntry2_ProtectedExt, SOOrderEntry>
    {
        #region Events

        public virtual void _(Events.FieldVerifying<SOLine.curyExtPrice> e, PXFieldVerifying baseMethod)
        {
            // Remove curyExtPrice validation(negative)
            // baseMethod();
            SOLine row = e.Row as SOLine;

            if (row == null)
                return;

            if (Base1.IsManualPriceFlagNeeded(e.Cache, row))
                row.ManualPrice = true;
        }

        public virtual void _(Events.FieldVerifying<SOLine.curyUnitPrice> e, PXFieldVerifying baseMethod)
        {
            // Remove curyUnitPrice validation(negative)
            // baseMethod();
            SOLine row = e.Row as SOLine;

            if (row == null)
                return;

            if (row.IsFree != true && Base.GetMinGrossProfitValidationOption(e.Cache, row) != MinGrossProfitValidationType.None)
            {
                e.Cache.RaiseExceptionHandling<SOLine.curyUnitPrice>(row, null, null);

                var mgpc = new MinGrossProfitClass
                {
                    DiscPct = row.DiscPct,
                    CuryDiscAmt = row.CuryDiscAmt,
                    CuryUnitPrice = (decimal?)e.NewValue
                };

                Base1.SOLineValidateMinGrossProfit(e.Cache, row, mgpc);

                e.NewValue = mgpc.CuryUnitPrice;
            }

            if (Base1.IsManualPriceFlagNeeded(e.Cache, row))
                row.ManualPrice = true;
        }

        #endregion
    }
}
