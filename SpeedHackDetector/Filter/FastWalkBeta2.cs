using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeedHackDetector.Network;
using System.Threading;

namespace SpeedHackDetector.Filter
{
    public class FastWalkBeta2
    {
        private static TimeSpan m_WalkMount = TimeSpan.FromSeconds(0.2);
        private static TimeSpan m_RunMount = TimeSpan.FromSeconds(0.1);
        private static TimeSpan m_WalkFoot = TimeSpan.FromSeconds(0.4);
        private static TimeSpan m_RunFoot = TimeSpan.FromSeconds(0.2);
        private String m_Username;
        private Queue<MovementRecord> m_MoveRecords;
        private Queue<MovementRecord> m_MoveRecordsFooted;
        private int[,] m_OldFooted;
        int m_InsertRow;
        int m_InsertColumns;
        private int m_PacketSequence = 6;
        private DateTime m_EndQueue;
        private DateTime m_EndQueueFooted;
        private int m_FastWalkMaxStepInTick = 4; //Il valore è da vedere se 3 o 4 quando non sono su macchina virtual
        private int m_Sequence;
        private Direction oldDirection;
        private Direction oldDirectionFooted;

        public int Sequence { get { return this.m_Sequence; } set { this.m_Sequence = value; } }

        public String Username { get { return this.m_Username; } }

        public FastWalkBeta2(String username)
        {
            this.m_Username = username;
            this.m_MoveRecords = new Queue<MovementRecord>(m_PacketSequence);
            this.m_MoveRecordsFooted = new Queue<MovementRecord>(m_PacketSequence);
            this.m_EndQueue = DateTime.Now;
            this.oldDirection = Direction.Down;
            this.m_InsertRow = 0;
            this.m_InsertColumns = 0;
            this.m_OldFooted = new int[m_PacketSequence, m_PacketSequence];
        }

        public void initOldFooted() 
        {
            for (int i = 0; i < m_PacketSequence; i++ )
            {
                for (int j = 0; j < m_PacketSequence; j++)
                {
                    m_OldFooted[j,i] = -1;
                }
                
            }
        }

        public bool checkFastWalk(Direction d)
        {
            bool res = false;
            SkipExpired(m_MoveRecords);
            SkipExpired(m_MoveRecordsFooted);

            insertOldMovementReecordFooted();
            checkFastWalk();

            TimeSpan delay = ComputeMovementSpeed(d);
            TimeSpan delyaFooted = ComputeMovementSpeedFooted(d);

            DateTime end;
            DateTime endFooted;

            if (m_MoveRecords.Count > 0)
                end = m_EndQueue + delay;
            else
                end = DateTime.Now + delay;
            m_EndQueue = end;

            if (m_MoveRecordsFooted.Count > 0)
                endFooted = m_EndQueueFooted + delyaFooted;
            else
                endFooted = DateTime.Now + delyaFooted;
            m_EndQueueFooted = endFooted;

            m_MoveRecords.Enqueue(MovementRecord.NewInstance(end));
            m_MoveRecordsFooted.Enqueue(MovementRecord.NewInstance(endFooted));
  
            return res;
        }

        private void insertOldMovementReecordFooted()
        {
            m_OldFooted[m_InsertRow, m_InsertColumns] = m_MoveRecordsFooted.Count;
            m_InsertColumns++;
            if (m_InsertColumns == m_PacketSequence - 1)
            {
                m_InsertColumns = 0;
                m_InsertRow++;
            }
            if (m_InsertRow == m_PacketSequence && m_InsertColumns == 0)
            {
                m_InsertRow = 0;
            }
        }

        private bool checkFastWalk()
        {
            if( checkFastWalkFooted())
            {
                Console.WriteLine("FAST FOOTED");
            }
            if (m_MoveRecords.Count >= m_FastWalkMaxStepInTick)
            {
                Console.WriteLine("FAST");
            }
            return false;
        }

        private bool checkFastWalkFooted()
        {
            if (m_OldFooted[m_PacketSequence - 1, m_PacketSequence - 1] == -1)
            {
                return false;
            }
            double [] averages = new double [m_PacketSequence];
            int[] maxs = new int [m_PacketSequence];
            for (int i = 0; i < m_PacketSequence; i++)
            {
                averages[i] = extractRow(i).Average();
                maxs[i] = extractRow(i).Max();
            }
            double totalAverage = averages.Average();
            if (totalAverage >= 2.3 && totalAverage <= 4.0)
            {
                if (maxs.Max() <= 6)
                {
                    return true;
                }
            }
            return false;
        }

        private bool maxStabile(int[] maxs)
        {
            bool res = false;
            int difference = 0;
            int i =0;
            for (i = 0; i < m_PacketSequence - 1 && ( difference < 3 && difference > -3); i++ )
            {
                difference = (maxs[i] + difference) - maxs[i + 1];
            }
            if (i == m_PacketSequence - 2) //ho fatto tutti i confornti e la differenza è [-2 , 2]
            {
                res = true;
            }
            return res;
        }

        private int[] extractRow(int row)
        {
            int [] res = new int[m_PacketSequence];
            for (int i = 0; i < m_PacketSequence; i++)
            {
                res[i] = m_OldFooted[row, i];
            }
            return res;
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
            else
            {
                Console.WriteLine("PIEDI");
                return (dir & Direction.Running) != 0 ? m_RunFoot : m_WalkFoot;
               
            }
        }

        private TimeSpan ComputeMovementSpeedFooted(Direction dir)
        {
            if ((dir & Direction.Mask) != (this.oldDirectionFooted & Direction.Mask))
            {
                this.oldDirectionFooted = dir;
                return TimeSpan.FromSeconds(0.1);	// We are NOT actually moving (just a direction change)
            }
            bool running = ((dir & Direction.Running) != 0);
            return (dir & Direction.Running) != 0 ? m_RunFoot : m_WalkFoot;
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
