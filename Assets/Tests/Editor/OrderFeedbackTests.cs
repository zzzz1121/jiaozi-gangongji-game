using NUnit.Framework;
using Jiaozi;
public class OrderFeedbackTests
{
    [Test]public void ThirdOrderSeparatesCurrentProgressFromTotal(){var r=new WorkshopRules();r.Reset(false);for(int i=0;i<2;i++){r.BeginFrame(0);r.Process(0,0,2,out _);r.Interact(0,0,out _);r.Interact(0,1,out _);r.BeginFrame(4);r.Interact(0,1,out _);r.Interact(0,3,out _);r.Process(0,3,2,out _);r.Interact(0,3,out _);r.Interact(0,4,out _);r.Process(0,4,1,out _);r.Interact(0,4,out _);r.Interact(0,6,out _);}StringAssert.Contains("第 3 单：已交 0/2 张",OrderFeedback.Summary(r));StringAssert.Contains("本局累计交付 2 张",OrderFeedback.Summary(r));StringAssert.Contains("已完成 2 单",OrderFeedback.Summary(r));StringAssert.Contains("需要 2 张",OrderFeedback.NewOrder(r));Assert.AreEqual(40,r.Score);}
}
