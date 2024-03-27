using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUMTomofunCustomization.LUMLibrary
{
    public static class TomofunPublicFunction
    {
        /// <summary>
        /// Setting Sales Order Tax information (TaxID, CuryTaxAmt, CuryTaxTotal, TaxzoneID)
        /// </summary>
        /// <param name="soGraph"></param>
        /// <param name="taxID"></param>
        /// <param name="taxZoneID"></param>
        /// <param name="taxAmt"></param>
        public static void SalesOrderTaxHandler(SOOrderEntry soGraph, string taxID, string taxZoneID, string taxAmt)
        {
            if (string.IsNullOrEmpty(soGraph.Document.Current?.TaxZoneID))
            {
                soGraph.Document.Current.OverrideTaxZone = true;
                soGraph.Document.Current.TaxZoneID = taxZoneID;
            }
            soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Cache.CreateInstance() as SOTaxTran;
            soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, taxID);
            //soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxZoneID>(soGraph.Taxes.Current, string.IsNullOrEmpty(soGraph.Taxes.Current?.TaxZoneID)? taxZoneID : soGraph.Taxes.Current?.TaxZoneID);
            soGraph.Taxes.Cache.SetValueExt<SOTaxTran.curyTaxAmt>(soGraph.Taxes.Current, taxAmt);
            soGraph.Taxes.UpdateCurrent();

            soGraph.Document.Cache.SetValueExt<SOOrder.curyTaxTotal>(soGraph.Document.Current, taxAmt);
        }

        /// <summary>
        /// Setting Sales Order Tax information (TaxID, TaxzoneID)
        /// </summary>
        /// <param name="soGraph"></param>
        /// <param name="taxID"></param>
        /// <param name="taxZoneID"></param>
        public static void SalesOrderTaxHandler(SOOrderEntry soGraph, string taxID, string taxZoneID)
        {
            if (string.IsNullOrEmpty(soGraph.Document.Current?.TaxZoneID))
            {
                soGraph.Document.Current.OverrideTaxZone = true;
                soGraph.Document.Current.TaxZoneID = taxZoneID;
            }
            soGraph.Taxes.Current = soGraph.Taxes.Current ?? soGraph.Taxes.Cache.CreateInstance() as SOTaxTran;
            soGraph.Taxes.Cache.SetValueExt<SOTaxTran.taxID>(soGraph.Taxes.Current, taxID);
            soGraph.Taxes.UpdateCurrent();
        }
    }
}
