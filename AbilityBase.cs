using System.Collections.Generic;
using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Utility;

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

        public bool IsLocked { get => _isLocked; }

        public virtual void SetLocked(bool state)
        {
            if (state == true && AffectedByNullifier == false)
                return;

            _isLocked = state;
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
    public class VirtualEnergyShield : AbstractPassiveEnergy
    {
        protected LimbBehaviour[] _limbs;
        protected List<EnergyLimb> _enerLimb = new List<EnergyLimb>();
        protected float _absorbMult = 1.01f;
        protected Color _absorbColor;
        protected bool _isSetedUp;
        protected ENSYS.EnergySystem sys;

        public override string DescTitle()
        {
            return "Energy Shield";
        }

        public override Sprite DescImage()
        {
            return null;
        }

        public override string Description()
        {
            return "This creature has an energy shield ability that allows it to block some of incoming Powers effects at the cost of Energy equal to the Energy used for that attack \nChance depends on Energy Ratio. Current chance : " + (sys.Energy  / sys.MaxEnergy) + "%";
        }

        // public bool IsSetedUp { get => IsSetedUp; protected set => IsSetedUp = value; }

        public Color AbsorbColor { get => _absorbColor; set => _absorbColor = value; }

        public bool IsSetedUp { get => _isSetedUp; protected set => _isSetedUp = value; }
        public virtual float AbsorbMult { get => Convert.ToInt32(_absorbMult); set => _absorbMult = value; }

        private void Start()
        {
            // this.GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons.Add(new ContextMenuButton("Toggle_Shield", "Toggle Energy Shield", "Toggle Energy Shield", () => { IsEnable = !IsEnable; }));
            sys = this.transform.root.gameObject.GetComponent<ENSYS.EnergySystem>();

            if (IsSetedUp == true)
                SetSystem(sys, _absorbColor, AbsorbMult);
        }

        public void SetSystem(ENSYS.EnergySystem system, Color absorbColor, float absorbMult = 1f)
        {
            EnergySys = system;
            AbsorbColor = absorbColor;
            AbsorbMult = absorbMult;

            _limbs = EnergySys.personBehaviour.Limbs;
            foreach (var limb in _limbs)
            {
                var energyProtected = limb.gameObject.GetOrAddComponent<EnergyLimb>();
                energyProtected.SetEnergyShield(this);
                _enerLimb.Add(energyProtected);
            }
            IsSetedUp = true;
        }

        public virtual bool TryApplyEffect(int cost, GameObject Obj, float Mult = 1f)
        {
            if (IsEnabled == false || _limbs[0].Person.IsAlive() == false) return true;

            if (CheckApply(cost, Mult) == false)
            {
                EnergySys.RemoveEnergy((int)(cost * (AbsorbMult * Mult)));

                 var effect = ModAPI.CreateParticleEffect("Vapor", Obj.transform.position);
                 effect.gameObject.GetComponent<ParticleSystem>().startColor = _absorbColor;
                return false;
            }
            return true;
        }

        public bool CheckApply(int cost, float Mult = 1f)
        {
            if (IsEnabled == false || _limbs[0].Person.IsAlive() == false) return true;

            if (EnergySys.Energy >= (int)(cost * (AbsorbMult * Mult)) && IsRandBlocked() == true)
            {
                return false;
            }

            return true;
        }

        protected virtual bool IsRandBlocked()
        {
            if (sys.MaxEnergy == 0)
                return false;

            return (Random.value < (sys.Energy / sys.MaxEnergy));
        }

        private void OnDestroy()
        {
            foreach (var ener in _enerLimb)
            {
                Destroy(ener);
            }
        }
    }

    public class EnergyLimb : MonoBehaviour
    {
        private VirtualEnergyShield _shield;

        public VirtualEnergyShield Shield { get => _shield; private set => _shield = value; }

        public void SetEnergyShield(VirtualEnergyShield shield)
        {
            _shield = shield;
        }
    }

}
