using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskPattern
{
    public delegate void FinishTask(AbstractTask abstractTask);

    public abstract class AbstractTask
    {
        private Thread m_Thread = null;
        public event FinishTask OnFinishTask;
        
        // Crea el hilo y le asigna FinishTask como delegado que ejecutará el proceso
        public void Execute()
        {
            m_Thread = new Thread(new ThreadStart(this.FinishTask));
            m_Thread.Start();
        }
        
        // Wait for Finish implementa el Join del hilo a procesar.
        public void WaitForFinish()
        {
            m_Thread?.Join(); // Ensure main thread waits for completion
        }
        
        // Esta es la funcion delegado que va a ejecutar el proceso abstracto a reemplazar
        private void FinishTask()
        {
            this.Process();
            OnFinishTask?.Invoke(this); // Invoke event handlers if set
        }
        
        // nuestro proceso abstracto
        protected abstract void Process();
    }
}