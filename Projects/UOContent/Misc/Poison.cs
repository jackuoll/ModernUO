using System;
using Server.Engines.Virtues;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;

namespace Server
{
    public class PoisonImpl : Poison
    {
        private readonly int m_Count;
        private readonly int m_CountMax;

        // Timers
        private readonly TimeSpan m_Delay;
        private readonly TimeSpan m_Interval;
        private readonly int m_MessageInterval;

        // Damage
        private readonly int m_Minimum;
        private readonly int m_Maximum;
        private readonly double m_Scalar;

        // AOS: min/max damage clamps
        public PoisonImpl(
            string name, int level, int min, int max, double percent, double delay, double interval, int count,
            int messageInterval
        )
        {
            Name = name;
            Level = level;
            m_Minimum = min;
            m_Maximum = max;
            m_Scalar = percent * 0.01;
            m_Delay = TimeSpan.FromSeconds(delay);
            m_Interval = TimeSpan.FromSeconds(interval);
            m_Count = count;
            m_CountMax = count;
            m_MessageInterval = messageInterval;
        }

        // Pre-AOS: no damage clamps, fixed tick count
        public PoisonImpl(
            string name, int level, double percent, double delay, double interval, int count,
            int messageInterval
        ) : this(name, level, percent, delay, interval, count, count, messageInterval)
        {
        }

        // Pre-AOS: no damage clamps, randomized tick count
        public PoisonImpl(
            string name, int level, double percent, double delay, double interval,
            int countMin, int countMax, int messageInterval
        )
        {
            Name = name;
            Level = level;
            m_Minimum = 0;
            m_Maximum = int.MaxValue;
            m_Scalar = percent * 0.01;
            m_Delay = TimeSpan.FromSeconds(delay);
            m_Interval = TimeSpan.FromSeconds(interval);
            m_Count = countMin;
            m_CountMax = countMax;
            m_MessageInterval = messageInterval;
        }

        public override string Name { get; }

        public override int Level { get; }

        // Damage formula from OSI source: percentage of current HP + 1 flat.
        // The percentages are the same across all eras. Only tick timing changed.
        //
        // Scalars (effective, after engine halving):
        //   Lesser 2.5%, Regular 3.33%, Greater 6.25%, Deadly 12.5%, Lethal 25%
        //
        // Pre-T2A: random(10,20) * strength ticks, intervals 15s/10s/10s/5s/5s
        // T2A:     10 ticks,                       intervals 15s/10s/10s/5s/5s
        // UOR+:    10 ticks,                       intervals 3s/3s/3s/4s/5s
        [CallPriority(10)]
        public static void Configure()
        {
            if (!Core.T2A)
            {
                // Pre-T2A: randomized tick counts = random(10, 20) * strength
                //                          name       lvl scalar  delay interval cntMin cntMax msgInterval
                Register(new PoisonImpl("Lesser",   0, 2.500,  15.0, 15.0, 10,  20,  3));
                Register(new PoisonImpl("Regular",  1, 3.330,  10.0, 10.0, 20,  40,  3));
                Register(new PoisonImpl("Greater",  2, 6.250,  10.0, 10.0, 30,  60,  3));
                Register(new PoisonImpl("Deadly",   3, 12.500, 5.0,  5.0,  40,  80,  3));
                Register(new PoisonImpl("Lethal",   4, 25.000, 5.0,  5.0,  50,  100, 3));
            }
            else if (!Core.UOR)
            {
                // T2A: fixed 10 ticks, same long intervals as pre-T2A
                //                          name       lvl scalar  delay interval count msgInterval
                Register(new PoisonImpl("Lesser",   0, 2.500,  15.0, 15.0, 10, 3));
                Register(new PoisonImpl("Regular",  1, 3.330,  10.0, 10.0, 10, 3));
                Register(new PoisonImpl("Greater",  2, 6.250,  10.0, 10.0, 10, 3));
                Register(new PoisonImpl("Deadly",   3, 12.500, 5.0,  5.0,  10, 3));
                Register(new PoisonImpl("Lethal",   4, 25.000, 5.0,  5.0,  10, 3));
            }
            else if (Core.AOS)
            {
                Register(new PoisonImpl("Lesser", 0, 4, 16, 7.5, 3.0, 2.25, 10, 4));
                Register(new PoisonImpl("Regular", 1, 8, 18, 10.0, 3.0, 3.25, 10, 3));
                Register(new PoisonImpl("Greater", 2, 12, 20, 15.0, 3.0, 4.25, 10, 2));
                Register(new PoisonImpl("Deadly", 3, 16, 30, 30.0, 3.0, 5.25, 15, 2));
                Register(new PoisonImpl("Lethal", 4, 20, 50, 35.0, 3.0, 5.25, 20, 2));
            }
            else
            {
                // UOR
                //                          name       lvl scalar  delay interval count msgInterval
                Register(new PoisonImpl("Lesser",   0, 2.500,  3.5, 3.0, 10, 2));
                Register(new PoisonImpl("Regular",  1, 3.330,  3.5, 3.0, 10, 2));
                Register(new PoisonImpl("Greater",  2, 6.250,  3.5, 3.0, 10, 2));
                Register(new PoisonImpl("Deadly",   3, 12.500, 3.5, 4.0, 10, 2));
                Register(new PoisonImpl("Lethal",   4, 25.000, 3.5, 5.0, 10, 2));
            }
        }

        public static Poison IncreaseLevel(Poison oldPoison)
        {
            var newPoison = oldPoison == null ? null : GetPoison(oldPoison.Level + 1);

            return newPoison ?? oldPoison;
        }

        public override Timer ConstructTimer(Mobile m) => new PoisonTimer(m, this);

        public class PoisonTimer : Timer
        {
            private readonly Mobile m_Mobile;
            private readonly PoisonImpl m_Poison;
            private readonly int m_Count;
            private int m_Index;
            private int m_LastDamage;

            public PoisonTimer(Mobile m, PoisonImpl p) : base(p.m_Delay, p.m_Interval)
            {
                From = m;
                m_Mobile = m;
                m_Poison = p;
                m_Count = p.m_Count == p.m_CountMax
                    ? p.m_Count
                    : Utility.Random(p.m_Count, p.m_CountMax - p.m_Count + 1);
            }

            public Mobile From { get; set; }

            protected override void OnTick()
            {
                if (Core.AOS && m_Poison.Level < 4 &&
                    TransformationSpellHelper.UnderTransformation(m_Mobile, typeof(VampiricEmbraceSpell)) ||
                    m_Poison.Level < 3 && OrangePetals.UnderEffect(m_Mobile) ||
                    AnimalForm.UnderTransformation(m_Mobile, typeof(Unicorn)))
                {
                    if (m_Mobile.CurePoison(m_Mobile))
                    {
                        // * You feel yourself resisting the effects of the poison *
                        m_Mobile.LocalOverheadMessage(MessageType.Emote, 0x3F, 1114441);

                        // * ~1_NAME~ seems resistant to the poison *
                        m_Mobile.NonlocalOverheadMessage(MessageType.Emote, 0x3F, 1114442, m_Mobile.Name);

                        Stop();
                        return;
                    }
                }

                if (m_Index++ == m_Count)
                {
                    m_Mobile.SendLocalizedMessage(502136); // The poison seems to have worn off.
                    m_Mobile.Poison = null;

                    Stop();
                    return;
                }

                int damage;

                if (!Core.AOS && m_LastDamage != 0 && Utility.RandomBool())
                {
                    damage = m_LastDamage;
                }
                else
                {
                    damage = 1 + (int)(m_Mobile.Hits * m_Poison.m_Scalar);

                    if (damage < m_Poison.m_Minimum)
                    {
                        damage = m_Poison.m_Minimum;
                    }
                    else if (damage > m_Poison.m_Maximum)
                    {
                        damage = m_Poison.m_Maximum;
                    }

                    m_LastDamage = damage;
                }

                if (Core.UOR)
                {
                    From?.DoHarmful(m_Mobile, true);
                }

                (m_Mobile as IHonorTarget)?.ReceivedHonorContext?.OnTargetPoisoned();

                AOS.Damage(m_Mobile, Core.UOR ? From : null, damage, 0, 0, 0, 100, 0);

                // OSI: randomly revealed between first and third damage tick, guessing 60% chance
                if (Utility.RandomDouble() < 0.40)
                {
                    m_Mobile.RevealingAction();
                }

                if (m_Index % m_Poison.m_MessageInterval == 0)
                {
                    if (!Core.UOR)
                    {
                        // Pre-UOR: level-specific bark messages
                        var level = m_Poison.Level;
                        m_Mobile.LocalOverheadMessage(MessageType.Emote, 0x3F, 1042857 + level * 2);
                        m_Mobile.NonlocalOverheadMessage(
                            MessageType.Emote, 0x3F, 1042858 + level * 2, m_Mobile.Name
                        );
                    }
                    else
                    {
                        m_Mobile.OnPoisoned(From, m_Poison, m_Poison);
                    }
                }
            }
        }
    }
}
