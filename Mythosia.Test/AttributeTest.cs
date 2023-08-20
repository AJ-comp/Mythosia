using Mythosia.Threading.Synchronization;

namespace Mythosia.Test
{
    public class AttributeTest
    {
        public async Task MyMethod1()
        {
            Console.WriteLine("MyMethod1 Start");
            await Task.Delay(2000);

//            throw new Exception();
            Console.WriteLine("MyMethod1 End");
        }

        public async Task MyMethod2()
        {
            Console.WriteLine("MyMethod2 Start");
            await Task.Delay(2000);
            Console.WriteLine("MyMethod2 End");
        }

        public async Task MyMethod3(string p)
        {
            Console.WriteLine("MyMethod3 Start");
            await Task.Delay(2000);
            Console.WriteLine("MyMethod3 End");
        }

        public async Task StartTest()
        {
            var test = new AttributeTest(); // AttributeTest 클래스의 인스턴스 생성

            Func<Task> t1 = test.MyMethod1;
            Func<Task> t2 = test.MyMethod2;

            SemaphoreSlim semaphore = new SemaphoreSlim(1);
            var task1 = semaphore.ExclusiveAsync(test.MyMethod1);
            var task3 = semaphore.ExclusiveAsync(test.MyMethod3, "abc");
            var task2 = semaphore.ExclusiveAsync(test.MyMethod2);
            await Task.WhenAll(task1, task2, task3);

            List<Task> listT = new List<Task>();
            try
            {
                for(int i=0; i<10; i++)
                {
                    listT.Add(t1());
                }
                for (int i = 0; i < 100; i++)
                {
                    listT.Add(t2());
                }

                await Task.WhenAll(listT.ToArray());
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("All methods completed.");
        }
    }
}
