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
            if (EnergySystemsUsers.ContainsKey(target.root) == true)
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
                newsystem.SetMaxEnergy( MaxEnergy);
                newsystem.SetRegen( RegenEnergy);
                newsystem.SetRegenTime( RegenTime);

                if (StartMax == true)
                {
                    newsystem.SetEnergy(MaxEnergy);
                }
                else
                {
                    newsystem.SetEnergy( 0);
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

        public UnityEvent OnEnableEvent = new UnityEvent();

        public bool isInitializated = false;

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

        public UnityEvent OnAwakeEvent = new UnityEvent();

        public void Awake()
        {
            try
            {
                HumanPerson = transform.root.GetComponent<PersonBehaviour>();
                if (HumanPerson == null)
                    Destroy(this);

                if (_cashedWait == null)
                    SetRegenTime(RegenTime);

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

        public UnityEvent<int> OnRemoveEnergyEvent = new UnityEvent<int>();

        public int GetEnergyAmount()
        {
            return Energy;
        }

        public bool IsInf()
        {
            return Infinity;
        }

        public void SetInf(bool IsInf)
        {
            Infinity = IsInf;
        }

        public int GetMaxEnergyAmount()
        {
            return MaxEnergy;
        }

        public Dictionary<string, object> ValuesHolder = new Dictionary<string, object>();

        public object GetValueFromHolder(string key)
        {
            if (ValuesHolder.ContainsKey(key))
            {
                return ValuesHolder[key];
            }
            return null;
        }

        public void AddValueToHolder(string key, object val)
        {
            if (ValuesHolder.ContainsKey(key) == false)
            {
                ValuesHolder.Add(key, val);
            }
            else
            {
                ValuesHolder[key] = val;
            }
        }

        public bool GetTag(string tag)
        {
            return tagLib.ContainsTag(tag);
        }

        public void AddTag(string tag)
        {
            tagLib.AddTag(tag);
        }

        public void RemoveTag(string tag)
        {
            tagLib.RemoveTag(tag);
        }

        public void RemoveEnergy(int amount)
        {
            if (Infinity == true) return;

            Energy = Mathf.Clamp(Energy -= amount, 0, MaxEnergy);
            OnRemoveEnergyEvent?.Invoke(amount);
        }

        public UnityEvent<int> OnAddEnergyEvent = new UnityEvent<int>();

        public void AddEnergy(int amount)
        {
            if (Infinity == true) return;
            Energy = Mathf.Clamp(Energy += amount, 0, MaxEnergy);
            OnAddEnergyEvent?.Invoke(amount);
        }

        public UnityEvent<int> OnAddMaxEnergyEvent = new UnityEvent<int>();

        public void AddMaxEnergy(int amount)
        {
            if (Infinity == true) return;

            MaxEnergy = Mathf.Clamp(MaxEnergy += amount, 0, 999999);
            OnAddMaxEnergyEvent?.Invoke(amount);
        }

        public UnityEvent<int> OnRemoveMaxEnergyEvent = new UnityEvent<int>();

        public void RemoveMaxEnergy(int amount)
        {
            if (Infinity == true) return;

            MaxEnergy = Mathf.Clamp(MaxEnergy -= amount, 0, 999999);
            OnRemoveMaxEnergyEvent?.Invoke(amount);
        }

        public UnityEvent<int> OnSetRegenEvent = new UnityEvent<int>();

        public void SetRegen(int value)
        {
            RegenEnergyAmount = value;
            OnSetRegenEvent?.Invoke(value);
        }

        public UnityEvent<int> OnSetMaxEvent = new UnityEvent<int>();

        public void SetMaxEnergy(int value)
        {
            MaxEnergy = value;
            OnSetMaxEvent?.Invoke(value);
        }

        public UnityEvent<int> OnSetEnergyEvent = new UnityEvent<int>();

        public void SetEnergy(int value)
        {
            Energy = value;
            OnSetEnergyEvent?.Invoke(value);
        }

        public UnityEvent OnUpdateEvent = new UnityEvent();

        private void Update()
        {
            OnUpdateEvent?.Invoke();
        }

        public float RegenTime = 1f;
        private WaitForSeconds _cashedWait;

        public UnityEvent<float> OnSetTimeRegenEvent = new UnityEvent<float>();

        public void SetRegenTime(float amount)
        {
            RegenTime = amount;
            _cashedWait = new WaitForSeconds(RegenTime);
            OnSetTimeRegenEvent?.Invoke(amount);
        }

        public UnityEvent OnRegenEvent = new UnityEvent();

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


        public UnityEvent OnDestroyEvent = new UnityEvent();

        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
            EnergySystemCore.EnergySystemsUsers.Remove(this.transform);
        }

        public UnityEvent OnDisableEvent = new UnityEvent();

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

            public override bool Equals(object obj)
            {
                if (obj is Tag otherTag)
                {
                    return Id.Equals(otherTag.Id, StringComparison.OrdinalIgnoreCase);
                }
                else if (obj is Enum otherEnumTag)
                {
                    return Id.Equals(otherEnumTag.ToString(), StringComparison.OrdinalIgnoreCase);
                }
                else if (obj is string otherStringTag)
                {
                    return Id.Equals(otherStringTag, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Id.ToLowerInvariant().GetHashCode();
            }

            public static implicit operator Tag(string id) => new Tag(id);
            public static implicit operator string(Tag tag) => tag.Id;
        }

        public List<Tag> TagList = new List<Tag>();

        public Tag AddEnumToTag(Enum enumValue)
        {
            return new Tag(enumValue.ToString());
        }

        public bool ContainsTag(Tag tag)
        {
            return TagList.Contains(tag);
        }

        public bool ContainsTag(string tagString)
        {
            return TagList.Contains(new Tag(tagString));
        }

        public void RemoveTag(Tag tag)
        {
            TagList.RemoveAll(t => t.Equals(tag));
        }

        public void RemoveTag(string tagString)
        {
            RemoveTag(new Tag(tagString));
        }

        public void AddTag(Tag tag)
        {
            if (!ContainsTag(tag))
            {
                TagList.Add(tag);
            }
        }

        public void AddTag(string tagString)
        {
            AddTag(new Tag(tagString));
        }

        public Tag FindTag(string tagString)
        {
            return TagList.Find(t => t.Equals(tagString));
        }
    }
}