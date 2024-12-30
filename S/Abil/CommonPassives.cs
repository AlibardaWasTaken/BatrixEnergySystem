using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;
using Random = UnityEngine.Random;

namespace ENSYS
{
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
            return "This creature has an energy shield ability that allows it to block some of incoming Powers effects at the cost of Energy equal to the Energy used for that attack \nChance depends on Energy Ratio. Current chance : " + (sys.Energy / sys.MaxEnergy) + "%";
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

    public abstract class AbstractRegenerationBasedOnEnergy : AbstractPassiveEnergy
    {
        protected PersonBehaviour _person;
        protected bool _skinRegen = false;
        protected float _basemult = 0f;
        public bool canRevive = false;

        public int EnergyPerRegen = 8;
        public float BaseTime = 5f;

        protected bool _isInitialized = false;
        protected WaitForSeconds waitFor;

        public void Init(EnergySystem sys, int energyPerRegen = 9, float baseTime = 5f, bool CanRevive = false)
        {
            EnergySys = sys;
            EnergyPerRegen = energyPerRegen;
            BaseTime = baseTime;
            canRevive = CanRevive;
            StartRegenCoroutine();

            _isInitialized = true;
        }

        public void SetBaseTime(float basetime)
        {
            BaseTime = basetime;
            waitFor = new WaitForSeconds(BaseTime);
        }

        public void StartRegenCoroutine()
        {
            _skinRegen = false;
            StartCoroutine(Regen());
        }

        protected abstract IEnumerator Regen();

        protected virtual void RemoveEnergyForRevival()
        {
            EnergySys.RemoveEnergy(RegenCostCalc());
        }

        protected virtual int RegenCostCalc()
        {
            return (EnergyPerRegen / 1 * Convert.ToInt32(_basemult));
        }

        protected virtual void RemoveEnergyForLimbRegen()
        {
            EnergySys.RemoveEnergy(EnergyPerRegen);
        }
    }

    public class RegenerationPassiveBase : AbstractRegenerationBasedOnEnergy
    {
        public override string DescTitle()
        {
            return "Regenerator";
        }

        private void Start()
        {
            _person = this.gameObject.GetComponent<LimbBehaviour>().Person;

            if (_isInitialized)
                Init(this.transform.root.gameObject.GetComponent<ENSYS.EnergySystem>(), EnergyPerRegen, BaseTime, canRevive);
        }

        private void OnEnable()
        {
            if (!_isInitialized)
                return;

            StartRegenCoroutine();
        }

        protected override IEnumerator Regen()
        {
            waitFor = null;
            while (true)
            {
                waitFor = new WaitForSeconds(BaseTime * EnergyMultRatio());
                yield return waitFor;

                if (_person.Limbs[0].IsDismembered == true || IsEnabled == false || IsLocked == true || (EnergySys.Energy < EnergyPerRegen * 6 && !EnergySys.Infinity))
                    continue;

                if (_person.IsAlive() || canRevive)
                {
                    if (!_person.IsAlive() && canRevive)
                    {
                        var clampedmult = Mathf.Clamp(2f + Random.Range(0, Convert.ToInt32(_basemult) / 2.5f), 1, 1024);
                        _basemult += clampedmult;
                        if (_person.Limbs[0].NodeBehaviour.IsConnectedToRoot)
                            _person.Limbs[0].RegenLimb();

                        RemoveEnergyForRevival();
                    }

                    if (_basemult > 1.1f)
                    {
                        _basemult -= Time.deltaTime * 3;
                    }
                    _person.Consciousness = 1f;
                    _person.ShockLevel = 0f;
                    _person.PainLevel = 0f;
                    _person.Braindead = false;
                    _person.BrainDamaged = false;

                    if (!_skinRegen)
                        StartCoroutine(SkinRegen());

                    foreach (LimbBehaviour Limb in _person.Limbs)
                    {
                        if (Limb.gameObject.activeSelf && Limb.NodeBehaviour.IsConnectedToRoot && !Limb.PhysicalBehaviour.OnFire)
                        {
                            if (Limb.Health < Limb.InitialHealth * 0.95f || Limb.SkinMaterialHandler.AcidProgress > 0.1f || Limb.PhysicalBehaviour.BurnProgress > 0.2f)
                                RemoveEnergyForLimbRegen();

                            Limb.RegenLimb();
                        }
                    }
                }
            }
        }

        private IEnumerator SkinRegen()
        {
            _skinRegen = true;
            float timer = 0;
            while (timer < 3f)
            {
                yield return null;
                timer += Time.deltaTime;
                foreach (LimbBehaviour Limb in _person.Limbs)
                {
                    if (Limb.gameObject.activeSelf && Limb.NodeBehaviour.IsConnectedToRoot && !Limb.PhysicalBehaviour.OnFire)
                    {
                        for (int i = 0; i < Limb.SkinMaterialHandler.damagePoints.Length; i++)
                            Limb.SkinMaterialHandler.damagePoints[i].z *= 1f - Time.deltaTime * 0.1f;

                        if (!Limb.IsZombie && Limb.SkinMaterialHandler.RottenProgress > 0.01f)
                            Limb.SkinMaterialHandler.RottenProgress -= Time.deltaTime * 0.05f;

                        if (Limb.SkinMaterialHandler.AcidProgress > 0.01f)
                            Limb.SkinMaterialHandler.AcidProgress -= Time.deltaTime * 0.05f;

                        if (Limb.PhysicalBehaviour.BurnProgress > 0.1f)
                            Limb.PhysicalBehaviour.BurnProgress -= Time.deltaTime * 0.02f;

                        Limb.SkinMaterialHandler.Sync();
                    }
                }
            }
            _skinRegen = false;
        }

        private float EnergyMultRatio()
        {
            return (1.02f - EnergyRatio());
        }

        private float EnergyRatio()
        {
            if (EnergySys.MaxEnergy == 0)
                return 0;

            return EnergySys.Energy / EnergySys.MaxEnergy;
        }

        public override Sprite DescImage()
        {
            return null;
        }

        public override string Description()
        {
            var basedesc = "This creature has regeneration ability :\nCost " + RegenCostCalc() + " Energy per damaged limb\nRegen Time " + string.Format("{0:0.00}", BaseTime * EnergyMultRatio()) + " (Affected by Energy Ratio)";
            if (canRevive)
            {
                basedesc += "\nCan regenerate after death";
                basedesc += "\n(Revival consume progressively more energy if it happens frequently)";
            }
            else
            {
                basedesc += "\nUnable to regenerate after death";
            }

            return basedesc;
        }
    }
}