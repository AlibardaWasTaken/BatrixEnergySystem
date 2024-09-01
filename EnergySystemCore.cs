using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ENSYS
{
    public static class EnergySystemCore
    {

        public static Dictionary<Transform, EnergySystem> EnergySystemsUsers = new Dictionary<Transform, EnergySystem>();
        public static Component GetEnergySystem(Transform target)
        {
            if(EnergySystemsUsers.ContainsKey(target.root) == true)
            {
                return EnergySystemsUsers[target.root];
            }
            else
            {
                return null;
            }
        }





        public static Component SetUpEnergySystem(Transform target, int MaxEnergy, int RegenEnergy, float RegenTime, bool StartMax, System.Action<GameObject> OnEndAction)
        {
            if (EnergySystemsUsers.ContainsKey(target.root) == false)
            {
                var newsystem = target.root.gameObject.AddComponent<EnergySystem>();
                EnergySystem.SetMaxEnergy(newsystem, MaxEnergy);
                EnergySystem.SetRegen(newsystem,RegenEnergy);
                EnergySystem.SetRegenTime(newsystem,RegenTime);

                if(StartMax == true)
                {
                    EnergySystem.SetEnergy(newsystem, MaxEnergy);
                }
                else
                {
                    EnergySystem.SetEnergy(newsystem, 0);
                }
                
                OnEndAction?.Invoke(target.gameObject);
                Debug.Log("Energy System inited");
                return newsystem;
            }
            else
            {
                return GetEnergySystem(target);
            }
        }

    }

    public class EnergySystem : MonoBehaviour
    {
        public bool AllowOverMax = false;
        public bool Infinity = false;
        public bool CanBeUpgraded = true;

        public TagLib tagLib;

        public int Energy;
        public int MaxEnergy;
        public int RegenEnergyAmount;

        public bool ShouldRegen = true;

        public PersonBehaviour HumanPerson;

        public UnityEvent OnEnableEvent;

        public bool isInitializated = false;

        //private List<AbstractPower> _powersList = new List<AbstractPower>();

        private void OnEnable()
        {
            if (isInitializated == false)
                return;

            OnEnableEvent?.Invoke();

            if (ShouldRegen == true)
                StartCoroutine(Regen());
        }

        private void Start()
        {
            isInitializated = true;
        }

        public UnityEvent OnAwakeEvent;

        public void Awake()
        {
            try
            {
                HumanPerson = transform.root.GetComponent<PersonBehaviour>();
                if (HumanPerson == null)
                    Destroy(this);

                if(_cashedWait == null)
                SetRegenTime(this,RegenTime);

                if (ShouldRegen == true)
                    StartCoroutine(Regen());


                

                foreach (var item in HumanPerson.Limbs)
                {
                    item.PhysicalBehaviour.ContextMenuOptions.Buttons.Add(new ContextMenuButton("EnergySYSTEMBATRIX_Toggle_Infinity", "Toggle Infinity Energy", "Toggle Infinity Energy", () => { Infinity = !Infinity; Energy = MaxEnergy; }));
                }

                if (EnergySystemCore.EnergySystemsUsers.ContainsKey(transform) == false)
               EnergySystemCore.EnergySystemsUsers.Add(transform, this);

                tagLib = this.gameObject.GetOrAddComponent<TagLib>();


                OnAwakeEvent?.Invoke();
            }
            catch (Exception e)
            {
                ModAPI.CreateParticleEffect("Spark", this.transform.root.position);
                Debug.Log("BATRIX ENERGY SYSTEM GOT ERROR ON AWAKE : " + e);
            }
        }

        public UnityEvent<int> OnRemoveEnergyEvent;


        public static int GetEnergyAmount(Component sys)
        {
            var ConvertedSys = sys as EnergySystem;
            return ConvertedSys.Energy;
        }

        public static int GetMaxEnergyAmount(Component sys)
        {
            var ConvertedSys = sys as EnergySystem;
            return ConvertedSys.MaxEnergy;
        }

        public static void RemoveEnergy(Component sys,int amount)
        {
            var ConvertedSys = sys as EnergySystem;
             

            if (ConvertedSys.Infinity == true) return;

            ConvertedSys.Energy = Mathf.Clamp(ConvertedSys.Energy -= amount, 0, ConvertedSys.MaxEnergy);
            ConvertedSys.OnRemoveEnergyEvent?.Invoke(amount);
        }

        public static UnityEvent<int> OnAddEnergyEvent;

        public static void AddEnergy(Component sys,int amount)
        {
            var ConvertedSys = sys as EnergySystem;
            if (ConvertedSys.Infinity == true) return;
            ConvertedSys.Energy = Mathf.Clamp(ConvertedSys.Energy += amount, 0, ConvertedSys.MaxEnergy);
            OnAddEnergyEvent?.Invoke(amount);
        }
       





        public UnityEvent<int> OnAddMaxEnergyEvent;






        public static void AddMaxEnergy(Component sys,int amount)
        {
            var ConvertedSys = sys as EnergySystem;
            if (ConvertedSys.Infinity == true) return;

            ConvertedSys.MaxEnergy = Mathf.Clamp(ConvertedSys.MaxEnergy += amount, 0, 999999);
            ConvertedSys.OnAddMaxEnergyEvent?.Invoke(amount);
        }

        public static UnityEvent<int> OnRemoveMaxEnergyEvent;

        public static void RemoveMaxEnergy(Component sys,int amount)
        {
            var ConvertedSys = sys as EnergySystem;
            if (ConvertedSys.Infinity == true) return;

            ConvertedSys.MaxEnergy = Mathf.Clamp(ConvertedSys.MaxEnergy -= amount, 0, 999999);
            OnRemoveMaxEnergyEvent?.Invoke(amount);
        }

        public UnityEvent<int> OnSetRegenEvent;

        public static void SetRegen(Component sys,int value)
        {
            var ConvertedSys = sys as EnergySystem;
            ConvertedSys.RegenEnergyAmount = value;
            ConvertedSys.OnSetRegenEvent?.Invoke(value);
        }

        public UnityEvent<int> OnSetMaxEvent;

        public static void SetMaxEnergy(Component sys,int value)
        {
            var ConvertedSys = sys as EnergySystem;
            ConvertedSys.MaxEnergy = value;
            ConvertedSys.OnSetMaxEvent?.Invoke(value);
        }

        public UnityEvent<int> OnSetEnergyEvent;

        public static void SetEnergy(Component sys,int value)
        {
            var ConvertedSys = sys as EnergySystem;
            ConvertedSys.Energy = value;
            ConvertedSys.OnSetEnergyEvent?.Invoke(value);
        }

        public UnityEvent OnUpdateEvent;

        private void Update()
        {
            OnUpdateEvent?.Invoke();
        }

        public float RegenTime = 1f;
        private WaitForSeconds _cashedWait;

        public UnityEvent<float> OnSetTimeRegenEvent;

        public static void SetRegenTime(Component sys,float amount)
        {
            var ConvertedSys = sys as EnergySystem;
            ConvertedSys.RegenTime = amount;
            ConvertedSys._cashedWait = new WaitForSeconds(ConvertedSys.RegenTime);
            ConvertedSys.OnSetTimeRegenEvent?.Invoke(amount);
        }

        public UnityEvent OnRegenEvent;

        private IEnumerator Regen()
        {
            yield return null;

            while (ShouldRegen)
            {
                if (Infinity == true) { yield return _cashedWait; continue; }

                if (HumanPerson.IsAlive())
                    Energy += RegenEnergyAmount;

                Energy = Mathf.Clamp(Energy, 0, !AllowOverMax ? MaxEnergy : 99999);
                OnRegenEvent?.Invoke();

                yield return _cashedWait;
            }
        }

        public Type GetTagLibType()
        {
            return tagLib.GetType();
        }

        public UnityEvent OnDestroyEvent;

        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
            EnergySystemCore.EnergySystemsUsers.Remove(this.transform);
        }

        public UnityEvent OnDisableEvent;

        private void OnDisable()
        {
            OnDisableEvent?.Invoke();
        }
    }

    public class TagLib : MonoBehaviour
    {
        public class Tag
        {
            public string Id { get; private set; }

            public Tag(string id)
            {
                Id = id;
            }

            // Override Equals and GetHashCode to compare Tags by ID
            public override bool Equals(object obj)
            {
                if (obj is Tag otherTag)
                {
                    return Id == otherTag.Id;
                }
                else if (obj is Enum otherEnumTag)
                {
                    return Id == otherEnumTag.ToString();
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }

        public Tag AddEnumToTag(Enum enumValue)
        {
            return new Tag(enumValue.ToString());
        }

        public List<Tag> TagList = new List<Tag>();

        public bool ContainsTag(Tag tag)
        {
            return TagList.Contains(tag);
        }

        public void RemoveTag(Tag tag)
        {
            if (TagList.Contains(tag))
            {
                TagList.Remove(tag);
            }
        }

        public void AddTag(Tag tag)
        {
            if (!TagList.Contains(tag))
            {
                TagList.Add(tag);
            }
        }
    }
}