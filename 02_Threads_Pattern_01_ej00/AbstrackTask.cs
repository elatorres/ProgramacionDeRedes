namespace TaskPattern
{
    public abstract class AbstrackTask
    {
        private Thread m_Thread = null;

        public void Execute()
        {
            m_Thread = new Thread(new ThreadStart(this.Process));
            m_Thread.Start();
        }
        // Cuando se llama a este método se bloquea el código hasta que termina de procesarse la tarea
        public void WaitForFinish()
        {
            if(m_Thread != null) m_Thread.Join();
        }
        
        // Cualquier tarea concreta que quiera ejecutarse en paralelo tiene que implementar este método
        protected abstract void Process();
    }
}