using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeedHackDetector.Network;
using System.Threading;

namespace SpeedHackDetector.Filter
{
    public class FastWalk : Filter<Direction>
    {
        private static TimeSpan m_WalkMount = TimeSpan.FromSeconds(0.2);
        private static TimeSpan m_RunMount = TimeSpan.FromSeconds(0.1);
        //private static TimeSpan m_WalkFoot = TimeSpan.FromSeconds(0.4);
        //private static TimeSpan m_RunFoot = TimeSpan.FromSeconds(0.2);
        private String m_Username;
        private Queue<MovementRecord> m_MoveRecords;
        private DateTime m_EndQueue;
        private int m_FastWalkMaxStepInTick = 3; //Il valore è da vedere se 3 o 4 quando non sono su macchina virtual
        private int m_Sequence;
        private Direction oldDirection;
        private bool m_Started;
        private System.Timers.Timer m_Timer;
        public int Sequence { get { return this.m_Sequence; } set { this.m_Sequence = value; } }

        public String Username { get { return this.m_Username; } }

        public FastWalk(String username)
        {
            this.m_Username = username;
            this.m_MoveRecords = new Queue<MovementRecord>(6);
            this.m_EndQueue = DateTime.Now;
            this.oldDirection = Direction.Down;
        }

        public void start()
        {
            m_Timer = new System.Timers.Timer();
            m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(setStarted);
            m_Timer.Interval = 5000;
            m_Timer.Enabled = true;
            m_Timer.AutoReset = false;
        }

        private void setStarted(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.m_Started = true;
            m_Timer.Stop();
            m_Timer.Dispose();
        }

        public bool DoFilter(Direction d)
        {
            bool res = false;
            if (m_Started)
            {
                
                SkipExpired(m_MoveRecords);


                res = checkFastWalk();

                TimeSpan delay = ComputeMovementSpeed(d);

                DateTime end;

                if (m_MoveRecords.Count > 0)
                    end = m_EndQueue + delay;
                else
                    end = DateTime.Now + delay;
                m_EndQueue = end;

                m_MoveRecords.Enqueue(MovementRecord.NewInstance(end));

            }
            return res;
        }

        private bool checkFastWalk()
        {
            if (m_MoveRecords.Count >= m_FastWalkMaxStepInTick)
            {
                return true;
            }
            return false;
        }

        private void SkipExpired(Queue<MovementRecord> queue)
        {
            while (queue.Count > 0)
            {
                MovementRecord r = queue.Peek();

                if (r.Expired())
                    queue.Dequeue();
                else
                    break;
            }
        }

        private TimeSpan ComputeMovementSpeed(Direction dir)
        {
            if ((dir & Direction.Mask) != (this.oldDirection & Direction.Mask))
            {
                this.oldDirection = dir;
                return TimeSpan.FromSeconds(0.1);	// We are NOT actually moving (just a direction change)
            }

            bool running = ((dir & Direction.Running) != 0);
            if (true) //check sul mounted
            {
                return (dir & Direction.Running) != 0 ? m_RunMount : m_WalkMount;
                
            }
        }


        private class MovementRecord
        {
            public DateTime m_End;

            private static Queue<MovementRecord> m_InstancePool = new Queue<MovementRecord>();

            public static MovementRecord NewInstance(DateTime end)
            {
                MovementRecord r;

                if (m_InstancePool.Count > 0)
                {
                    r = m_InstancePool.Dequeue();

                    r.m_End = end;
                }
                else
                {
                    r = new MovementRecord(end);
                }

                return r;
            }

            private MovementRecord(DateTime end)
            {
                m_End = end;
            }

            public bool Expired()
            {
                bool v = (DateTime.Now >= m_End);

                if (v)
                    m_InstancePool.Enqueue(this);

                return v;
            }
        }

        public void Reset()
        {
            if (m_MoveRecords != null && m_MoveRecords.Count > 0)
                m_MoveRecords.Clear();

            m_EndQueue = DateTime.Now;
        }
    }
}
