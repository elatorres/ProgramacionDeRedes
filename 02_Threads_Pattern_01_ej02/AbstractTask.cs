namespace TaskPattern
{
    // Definimos la foirma del delegado para cuando este sea definido
    // [La firma de la(s) funcion(es) que se van a llamar cuando finalice la tarea]
    public delegate void FinishTask(AbstractTask abstractTask);
    public abstract class AbstractTask
    {
        private Thread m_Thread = null;
        // La palabra clave event se usa para declarar un evento en una clase de publicador.
        public event FinishTask OnFinishTask = null;
        public void Execute()
        {
            m_Thread = new Thread(new ThreadStart(this.FinishTask));
            m_Thread.Start();
        }
        
        public void WaitForFinish()
        {
            if(m_Thread != null) m_Thread.Join();
        }

        private void FinishTask()
        {
            this.Process();
            // cuando llegue a esta línea this.Process terminó y se va a llamar los eventos OnFinishTask
            if (OnFinishTask != null) OnFinishTask.Invoke(this);
        }
        // Cualquier tarea concreta que quiera ejecutarse en paralelo tiene que implementar este método
        protected abstract void Process();
    }
}