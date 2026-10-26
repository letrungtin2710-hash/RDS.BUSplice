using System.Collections.Generic;
using Tekla.Structures.Plugins;

namespace RDS.BUSplice
{
    /// <summary>
    /// Du lieu dialog. Ten StructuresField = AttributeName cua control trong MainForm (giu y het).
    /// Ten trung voi field trong Settings (src/Settings.cs) de engine dung chung voi macro.
    /// </summary>
    public class PluginData
    {
        // ---------- Endplate 1 (rafter nho) / 2 (rafter lon): mep tinh tu bolt ngoai cung ----------
        [StructuresField("Ep1T")] public double Ep1T;
        [StructuresField("Ep1Top")] public double Ep1Top;
        [StructuresField("Ep1Bot")] public double Ep1Bot;
        [StructuresField("Ep2T")] public double Ep2T;
        [StructuresField("Ep2Top")] public double Ep2Top;
        [StructuresField("Ep2Bot")] public double Ep2Bot;
        [StructuresField("EpLeft")] public double EpLeft;     // mep trai / phai dung chung 2 endplate
        [StructuresField("EpRight")] public double EpRight;

        // ---------- Bolt ----------
        [StructuresField("BoltVertRef")] public int BoltVertRef;          // 0 = Top, 1 = Middle (giong Joining plates 14)
        [StructuresField("BoltTopOffset")] public double BoltTopOffset;   // Top: hang tren cung -> top rafter nho; Middle: tam nhom bolt -> giua rafter nho (+ = len)
        [StructuresField("BoltDistDown")] public string BoltDistDown;     // list khoang cach hang, tinh xuong
        [StructuresField("BoltDistAcross")] public string BoltDistAcross; // list khoang cach cot
        [StructuresField("BoltDelete")] public string BoltDelete;         // so bolt bi xoa
        [StructuresField("BoltSize")] public double BoltSize;
        [StructuresField("BoltStandard")] public string BoltStandard;
        [StructuresField("BoltTolerance")] public double BoltTolerance;

        // ---------- Flange (1) ----------
        [StructuresField("FlangeOn")] public int FlangeOn;                // 0 = No, 1 = Yes
        [StructuresField("FlangeProfile")] public string FlangeProfile;
        [StructuresField("FlangeLength")] public double FlangeLength;

        // ---------- Stiffener (2) ----------
        [StructuresField("StiffOn")] public int StiffOn;                  // 0 = No, 1 = Yes
        [StructuresField("StiffT")] public double StiffT;
        [StructuresField("StiffB")] public double StiffB;                 // 0 = tu tinh toi mep flange rafter lon
        [StructuresField("StiffL")] public double StiffL;
        [StructuresField("StiffChamX")] public double StiffChamX;
        [StructuresField("StiffChamY")] public double StiffChamY;

        // ---------- Parts (Class trong = lay theo web rafter; Finish luon theo web rafter) ----------
        [StructuresField("EpName")] public string EpName;
        [StructuresField("EpPrefix")] public string EpPrefix;
        [StructuresField("EpStart")] public int EpStart;
        [StructuresField("EpMaterial")] public string EpMaterial;
        [StructuresField("EpClass")] public string EpClass;

        [StructuresField("FlName")] public string FlName;
        [StructuresField("FlPrefix")] public string FlPrefix;
        [StructuresField("FlStart")] public int FlStart;
        [StructuresField("FlMaterial")] public string FlMaterial;
        [StructuresField("FlClass")] public string FlClass;

        [StructuresField("StName")] public string StName;
        [StructuresField("StPrefix")] public string StPrefix;
        [StructuresField("StStart")] public int StStart;
        [StructuresField("StMaterial")] public string StMaterial;
        [StructuresField("StClass")] public string StClass;
    }

    /// <summary>Gia tri mac dinh cho ComboBox (index) - dat trong constructor MainForm.</summary>
    public static class Defaults
    {
        public static readonly Dictionary<string, object> Values = new Dictionary<string, object>
        {
            { "FlangeOn", 1 },
            { "StiffOn", 1 },
            { "BoltVertRef", 0 },
        };
    }
}
