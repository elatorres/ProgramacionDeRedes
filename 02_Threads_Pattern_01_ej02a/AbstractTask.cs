using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskPattern
{
    // Delegada para notificar que la tarea terminó
    public delegate void FinishTask(AbstractTask abstractTask);
    // Este usa --==Tasks==--
    public abstract class AbstractTask
    {
        private Task m_Task = null; // esta es la tarea que se ejecuta asincrónicamente
        public event FinishTask OnFinishTask;  // es un hook que se dispara cuando la tarea termina

        public void Execute()
        {
            m_Task = Task.Run(() => FinishTask());  // La tarea no hace nada sólo llama a FinishTask
            // No espera. Es tipo fire-and-forget pero con call-back al terminar.
        }

        public void WaitForFinish()
        {
            m_Task?.Wait();  // método para esperar si se desea esperar
        }

        private void FinishTask()
        {
            this.Process();  // invoca al process que es el que hace el trabajo. Al terminar ...
            OnFinishTask?.Invoke(this); // Invoke event handlers if any
        }

        protected abstract void Process();  // Este es el proceso abstracto a implementar.
    }
}