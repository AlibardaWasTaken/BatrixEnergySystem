using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;
using Random = UnityEngine.Random;

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
        public bool CanBeStolen = true;

        public bool CanBeEarased = true;
        public bool AffectedByNullifier = true;

        public int ActivationCost;

        public bool IsTemp = false;
        private bool _isLocked = false;
        public bool IsInited;
        public bool BypassDefence = false;
        public bool IsPowerActivated = true;
        public bool IsLocked { get => _isLocked; }

        public virtual void SetLocked(bool state)
        {
            if (state == true && AffectedByNullifier == false)
                return;

            _isLocked = state;
        }

        /// <summary>
        /// Turn on/off power
        /// </summary>
        public virtual void Switch()
        {
            if (IsLocked == true)
                return;

            IsPowerActivated = !IsPowerActivated;

            OnSwitch();
        }

        protected virtual void OnSwitch()
        {
        }

        public abstract Sprite DescImage();
        public abstract string Description();
        public abstract string DescTitle();
        public abstract void Nullify();
    }


    public abstract class AbstractPassive : BasePassive
    {

        public override void Nullify()
        {
            if (CanBeDeleted == true)
            {
                Destroy(this);
            }
        }
    }

    public abstract class AbstractPassiveEnergy : AbstractPassive
    {
        private ENSYS.EnergySystem _energySys;

        public ENSYS.EnergySystem EnergySys { get => _energySys; protected set => _energySys = value; }
    }

    public abstract class AbstractDebuff : MonoBehaviour
    {
        public bool DeletesOnEnd = true;
        public bool CanBePurged = true;
        public bool ShouldExpire = true;

        public float timer = 0f;
        public float timeForExpire = 5f;

        public void OnEnable()
        {
            if (this.gameObject.HaveTag("DebuffImmune"))
            {
                Expire();
            }
        }

        public virtual void Expire()
        {
            if (DeletesOnEnd == true)
            {
                Destroy(this);
            }
        }
    }


}
