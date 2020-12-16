using System.Collections.Generic;
using System.IO;
using System.Linq;

public record Seller : Goods
{
    //卖方客户
    public string 卖方客户 { get; set; }
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
    public Seller()
    {

    }
    public Seller(Result r)
    {
        var g = Goods.GoodsDict[r.货物编号];
        产地 = g.产地;
        仓库 = g.仓库;
        品牌 = g.品牌;
        品种 = g.品种;
        年度 = g.年度;
        等级 = g.等级;
        类别 = g.类别;
        货物编号 = r.货物编号;
        货物数量 = r.分配货物数量;
        卖方客户 = r.卖方客户;
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

    public static void SaveSellerAssignNumber(string filename, List<Seller> sellers)
    {
        var sw = new StreamWriter(filename, false, System.Text.Encoding.GetEncoding("GB2312"));
        foreach (var seller in sellers)
        {
            sw.WriteLine(seller.卖方客户 + "," + seller.货物编号 + "," + seller.货物数量 + "," + seller.已分配货物数量);
        }
        sw.Close();
    }

    public static List<(string 卖方客户, string 货物编号, int 货物数量, int 已分配货物数量)> LoadSellerAssignNumber(string filename)
    {
        var sr = new StreamReader(filename, System.Text.Encoding.GetEncoding("GB2312"));
        var rtn = new List<(string 卖方客户, string 货物编号, int 货物数量, int 已分配货物数量)>();
        while (!sr.EndOfStream)
        {
            var info = sr.ReadLine().Split(",");
            rtn.Add((info[0], info[1], int.Parse(info[2]), int.Parse(info[3])));
        }
        sr.Close();
        return rtn;
    }

    public Seller Copy()
    {
        var clone = this.MemberwiseClone() as Seller;
        return clone;
    }

    public double Antiffy;

    public void SetAntiffy(List<Buyer> buyers)
    {
        buyers = buyers.Where(x => x.第一意向.hopeType != enmHope.无 && this.IsMatchHope(x.第一意向)).ToList();
        Antiffy = buyers.Average(x => x.GetHopeScore(this));
    }
}