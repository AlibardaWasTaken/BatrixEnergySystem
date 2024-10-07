using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ENSYS
{
    public abstract class AbstractEnergySystem : MonoBehaviour
    {
        public bool Infinity { get; set; } = false;
        public bool CanBeUpgraded { get; set; } = true;

        protected int energy;
        public virtual int Energy
        {
            get => energy;
            set => energy = Mathf.Clamp(value, 0, MaxEnergy);
        }

        protected int maxEnergy;
        public virtual int MaxEnergy
        {
            get => maxEnergy;
            set
            {
                maxEnergy = Mathf.Clamp(value, 0, int.MaxValue);
                if (Energy > maxEnergy)
                    Energy = maxEnergy;
            }
        }

        public int RegenEnergyAmount { get; protected set; }
        public float RegenTime { get; protected set; } = 1f;

        public bool ShouldRegen { get; protected set; } = true;

        public PersonBehaviour personBehaviour;

        public UnityEvent OnEnableEvent { get; } = new UnityEvent();
        public UnityEvent OnAwakeEvent { get; } = new UnityEvent();
        public UnityEvent<float> OnRemoveEnergyEvent { get; } = new UnityEvent<float>();
        public UnityEvent<float> OnAddEnergyEvent { get; } = new UnityEvent<float>();
        public UnityEvent<float> OnSetMaxEnergyEvent { get; } = new UnityEvent<float>();
        public UnityEvent<float> OnSetEnergyEvent { get; } = new UnityEvent<float>();
        public UnityEvent OnRegenEvent { get; } = new UnityEvent();
        public UnityEvent OnDestroyEvent { get; } = new UnityEvent();
        public UnityEvent OnDisableEvent { get; } = new UnityEvent();

        protected Dictionary<string, object> ValuesHolder = new Dictionary<string, object>();

        public object GetValueFromHolder(string key)
        {
            if (ValuesHolder.TryGetValue(key, out object value))
            {
                return value;
            }
            return null;
        }

        public void AddValueToHolder(string key, object val)
        {
            ValuesHolder[key] = val;
        }

        protected virtual void Awake()
        {
            personBehaviour = GetComponent<PersonBehaviour>();
            OnAwakeEvent?.Invoke();
        }

        protected virtual void OnEnable()
        {
            OnEnableEvent?.Invoke();
            if (ShouldRegen)
                StartCoroutine(Regen());
        }

        public virtual void RemoveEnergy(int amount)
        {
            if (Infinity) return;
            Energy -= amount;
            OnRemoveEnergyEvent?.Invoke(amount);
        }


        public virtual void SetRegen(int amont)
        {
            RegenEnergyAmount = amont;
        }
        public virtual void AddEnergy(int amount)
        {
            if (Infinity) return;
            Energy += amount;
            OnAddEnergyEvent?.Invoke(amount);
        }

        public virtual void AddMaxEnergy(int amount)
        {
            if (Infinity) return;
            maxEnergy += amount;
        }

        public virtual void RemoveMaxEnergy(int amount)
        {
            if (Infinity) return;
            maxEnergy -= amount;
        }


        public virtual void SetMaxEnergy(int value)
        {
            MaxEnergy = value;
            OnSetMaxEnergyEvent?.Invoke(value);
        }

        public virtual void SetEnergy(int value)
        {
            Energy = value;
            OnSetEnergyEvent?.Invoke(value);
        }

        protected virtual IEnumerator Regen()
        {
            while (ShouldRegen)
            {
                if (!Infinity)
                {
                    Energy += RegenEnergyAmount;
                    OnRegenEvent?.Invoke();
                }
                yield return new WaitForSeconds(RegenTime);
            }
        }

        protected virtual void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
        }

        protected virtual void OnDisable()
        {
            OnDisableEvent?.Invoke();
        }

        public abstract bool Take(int amount);
        public abstract void Push(int amount);
    }

    public class EnergySystem : AbstractEnergySystem
    {
        public bool AllowOverMax = false;
        public TagLib tagLib;

        public bool isInitialized = false;

        private WaitForSeconds _cashedWait;

        protected override void Awake()
        {
            base.Awake();

            if (ENSYSCore.EnergySystemsUsers.ContainsKey(transform) == false)
                ENSYSCore.EnergySystemsUsers.Add(transform, this);

            tagLib = this.gameObject.GetOrAddComponent<TagLib>();

            SetRegenTime(RegenTime);

            AfterAwake();
        }

        public override int Energy
        {
            get => base.Energy;
            set
            {
                if (Infinity)
                    base.Energy = MaxEnergy;
                else
                    base.Energy = Mathf.Clamp(value, 0, AllowOverMax ? int.MaxValue : MaxEnergy);
            }
        }

        public virtual void AfterAwake() { }

        public virtual void OnUpdate() { }

        public virtual void SetRegenTime(float amount)
        {
            RegenTime = amount;
            _cashedWait = new WaitForSeconds(RegenTime);
        }

        protected override IEnumerator Regen()
        {
            yield return null;

            while (ShouldRegen)
            {
                if (Infinity) { yield return _cashedWait; continue; }

                if (personBehaviour.IsAlive())
                    Energy += RegenEnergyAmount;

                OnRegenEvent?.Invoke();

                yield return _cashedWait;
            }
        }

        public override bool Take(int amount)
        {
            if (amount <= Energy)
            {
                RemoveEnergy(amount);
                return true;
            }
            return false;
        }

        public override void Push(int amount)
        {
            AddEnergy(amount);
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


    public class UiInitilizedTagged : MonoBehaviour
    {

    }
}
