namespace _Game.Scripts.DesignPattern.Observer
{
    public interface IObserver
    {
       
        public virtual void OnNotify(){}
        public virtual void OnNotify(string action){}
    
        public virtual void OnNotify(IGameEvent @event){}
        
        // public void OnNotify(int id);

        // public void OnNotify(Data eventData);
    }
}