using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeedHackDetector.Network;

namespace SpeedHackDetector.Filter
{
    public class FastWalk
    {
        private static TimeSpan m_WalkMount = TimeSpan.FromSeconds(0.2);
        private static TimeSpan m_RunMount = TimeSpan.FromSeconds(0.1);
        private String m_Username;
        private Queue<MovementRecord> m_MoveRecords;
        private DateTime m_EndQueue;
        private int m_FastWalkMaxStepInTick = 3; //Il valore è da vedere se 3 o 4 quando non sono su macchina virtual
        private int m_Sequence;
        private Direction oldDirection;

        public int Sequence { get { return this.m_Sequence; } set { this.m_Sequence = value; } }

        public String Username { get { return this.Username; } }

        public FastWalk(String username) 
        {
            this.m_Username = username;
            this.m_MoveRecords = new Queue<MovementRecord>();
            this.m_EndQueue = DateTime.Now;
            this.oldDirection = Direction.Down;
        }

        public bool checkFastWalk(Direction d) {
            bool res = false;
            if( m_MoveRecords == null )
							m_MoveRecords = new Queue<MovementRecord>( 6 );

						while( m_MoveRecords.Count > 0 )
						{
							MovementRecord r = m_MoveRecords.Peek();

							if( r.Expired() )
								m_MoveRecords.Dequeue();
							else
								break;
						}

                        if (m_MoveRecords.Count >= m_FastWalkMaxStepInTick)
						{
                            res = true;
						}

						TimeSpan delay = ComputeMovementSpeed( d );

						DateTime end;

						if( m_MoveRecords.Count > 0 )
							end = m_EndQueue + delay;
						else
							end = DateTime.Now + delay;

						m_MoveRecords.Enqueue( MovementRecord.NewInstance( end ) );

						m_EndQueue = end;
                        return res;
        }

        private TimeSpan ComputeMovementSpeed(Direction dir)
        {
            if ((dir & Direction.Mask) != (this.oldDirection & Direction.Mask))
                return TimeSpan.FromSeconds(0.1);	// We are NOT actually moving (just a direction change)

            bool running = ((dir & Direction.Running) != 0);

            //bool onHorse = (this.Mount != null);

            return (running ? m_RunMount : m_WalkMount);

            //return (running ? m_RunFoot : m_WalkFoot);
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

       public virtual void ClearFastwalkStack()
       {
           if (m_MoveRecords != null && m_MoveRecords.Count > 0)
               m_MoveRecords.Clear();

           m_EndQueue = DateTime.Now;
       }
    }
}
