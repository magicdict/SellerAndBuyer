using System.Collections.Generic;
using System.Linq;

public static class Adjust
{
    public static void Run(string filename){
        var rs = Result.ReadFromCSV(filename);
        //按照卖家信息GroupBy
        var rs_CF = rs.Where(x=>x.品种 == "CF").ToList();
        var rs_SR = rs.Where(x=>x.品种 == "SR").ToList();
        System.Console.WriteLine("开始优化CF数据：");
        Optiomize(rs_CF);
        System.Console.WriteLine("开始优化SR数据：");
        Optiomize(rs_SR);
    }

    public static void Optiomize(List<Result> rs){
        var buyers = rs.GroupBy(x=>x.买方客户);

        //使用多个仓库的买家数
        var multi_repo_cnt =  buyers.Count(x=>x.ToList().Select(x=>x.仓库).Distinct().Count() > 1);
        System.Console.WriteLine("使用多个仓库的买家数：" + multi_repo_cnt);

    }
}   