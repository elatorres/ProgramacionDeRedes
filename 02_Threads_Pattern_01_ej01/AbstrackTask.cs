namespace TaskPattern
{
    public delegate void FinishTask(AbstractTask abstractTask);
    public abstract class AbstractTask
    {
        private Thread m_Thread = null;
        // Puntero a la función que se va a llamar al finalizar la tarea
        private FinishTask m_CallBack = null;
        // Le pasamos como parámetro la función callback

        public void Execute(FinishTask callback)
        {
            m_CallBack = callback;
            // La función this.FinishTask se va a ejecutar en paralelo
            m_Thread = new Thread(new ThreadStart(this.FinishTask));
            m_Thread.Start();
        }
        // Cuando se llama a este método se bloquea el código hasta que termina de procesarse la tarea
        public void WaitForFinish()
        {
            if(m_Thread != null) m_Thread.Join();
        }

        private void FinishTask()
        {
            this.Process();
            if (m_CallBack != null) m_CallBack.Invoke(this);
        }
        // Cualquier tarea concreta que quiera ejecutarse en paralelo tiene que implementar este método
        protected abstract void Process();
    }
}