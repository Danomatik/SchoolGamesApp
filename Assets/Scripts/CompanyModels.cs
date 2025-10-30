using System;
using System.Collections.Generic;

[Serializable]
public class CompanyConfigData
{
    public int companyID;
    public string companyName;
    public int costFound;
    public int costInvest;
    public int costAG;
    public int revenueFound;
    public int revenueInvest;
    public int revenueAG;
}

[Serializable]
public class CompanyConfigCollection
{
    public List<CompanyConfigData> companies;
}

public enum CompanyLevel
{
    None,
    Founded,
    Invested,
    AG
}

[Serializable]
public class CompanyField
{
    public int fieldIndex;     // 0..39
    public int companyID;      // aus JSON
    public int ownerID = -1;   // -1 = niemand
    public CompanyLevel level = CompanyLevel.None;
}
