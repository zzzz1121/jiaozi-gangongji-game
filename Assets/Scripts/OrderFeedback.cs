namespace Jiaozi
{
    public static class OrderFeedback
    {
        public static string Summary(WorkshopRules rules)=>$"第 {rules.Orders+1} 单：已交 {rules.Received}/{rules.Required} 张    剩余 {UnityEngine.Mathf.CeilToInt(rules.Remaining)} 秒\n本局累计交付 {rules.Tickets} 张    已完成 {rules.Orders} 单    得分 {rules.Score}";
        public static string NewOrder(WorkshopRules rules)=>$"上一单完成！第 {rules.Orders+1} 单到达，需要 {rules.Required} 张朱印交子。";
    }
}
