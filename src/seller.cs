using System.Collections.Generic;
using System.IO;
public record Seller
{
    //卖方客户
    public string 卖方客户 { get; set; }
    //品种
    public string 品种 { get; set; }
    //货物编号
    public string 货物编号 { get; set; }
    //货物数量（张）
    public int 货物数量 { get; set; }
    //被分配数量
    public int 已分配货物数量 { get; set; }
    //剩余数量
    public int 剩余货物数量
    {
        get
        {
            return 货物数量 - 已分配货物数量;
        }
    }
    public bool 是否分配完毕
    {
        get
        {
            return 剩余货物数量 == 0;
        }
    }
    //仓库
    public string 仓库 { get; set; }
    //品牌
    public string 品牌 { get; set; }
    //产地
    public string 产地 { get; set; }
    //年度
    public string 年度 { get; set; }
    //等级
    public string 等级 { get; set; }
    //类别
    public string 类别 { get; set; }

    public bool IsMatchHope((enmHope, string) hope)
    {
        switch (hope.Item1)
        {
            case enmHope.仓库:
                return 仓库.Equals(hope.Item2);
            case enmHope.品牌:
                return 品牌.Equals(hope.Item2);
            case enmHope.产地:
                return 产地.Equals(hope.Item2);
            case enmHope.年度:
                return 年度.Equals(hope.Item2);
            case enmHope.等级:
                return 等级.Equals(hope.Item2);
            case enmHope.类别:
                return 类别.Equals(hope.Item2);
            default:
                return false;
        }
    }

    int Hope_Score(Buyer buyer)
    {
        return Utility.GetHopeScore(buyer, this);
    }


    public static List<Seller> ReadSellerFile(string filename)
    {
        var sellers = new List<Seller>();
        var sr = new StreamReader(filename, System.Text.Encoding.GetEncoding("GB2312"));
        sr.ReadLine();  //去除标题
        while (!sr.EndOfStream)
        {
            var infos = sr.ReadLine().Split(",");
            sellers.Add(new Seller()
            {
                卖方客户 = infos[0],
                品种 = infos[1],
                货物编号 = infos[2],
                货物数量 = int.Parse(infos[3]),
                仓库 = infos[4],
                品牌 = infos[5],
                产地 = infos[6],
                年度 = infos[7],
                等级 = infos[8],
                类别 = infos[9],
            });
        }
        sr.Close();
        System.Console.WriteLine("卖家件数：" + sellers.Count);
        return sellers;
    }
}