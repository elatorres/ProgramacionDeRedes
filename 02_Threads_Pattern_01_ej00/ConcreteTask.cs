namespace TaskPattern
{
    public class ConcreteTask : AbstrackTask
    {
        // Ejemplo de una operación que quiero ejecutar en paralelo
        protected override void Process()
        {
            Console.WriteLine("Starting Concrete task!");
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(100);
                Console.WriteLine("Running task Iteration: " + i);
            }

            Console.WriteLine("Finishing Concrete task!");
        }
    }
}