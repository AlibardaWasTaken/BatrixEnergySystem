using UnityEngine;

namespace ENSYS
{
    public abstract class BasePassive : MonoBehaviour, INullable, IDescriptable
    {
        public bool CanBeDeleted = true;
        public bool IsEnabled = true;
        public bool IsLocked = false;

        public bool CanBeDisabled = true;
        public bool DisplaysInMenu = true;

        public void Switch()
        {
            IsEnabled = !IsEnabled;
            OnSwitch();
        }

        public virtual void OnSwitch()
        {
        }

        public abstract Sprite DescImage();
        public abstract string Description();
        public abstract string DescTitle();
        public virtual void Nullify()
        {

        }
    }


    public abstract class BasePower : MonoBehaviour, INullable, IDescriptable
    {
        public abstract Sprite DescImage();
        public abstract string Description();
        public abstract string DescTitle();
        public abstract void Nullify();
    }







}
