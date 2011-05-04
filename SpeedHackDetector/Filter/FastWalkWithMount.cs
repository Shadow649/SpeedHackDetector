using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeedHackDetector.Network;
using System.Threading;

namespace SpeedHackDetector.Filter
{
    public class FastWalkWithmount
    {
        private static TimeSpan m_WalkMount = TimeSpan.FromSeconds(0.2);
        private static TimeSpan m_RunMount = TimeSpan.FromSeconds(0.1);
        private static TimeSpan m_WalkFoot = TimeSpan.FromSeconds(0.4);
        private static TimeSpan m_RunFoot = TimeSpan.FromSeconds(0.2);
        private String m_Username;
        private Queue<MovementRecord> m_MoveRecordsMounted;
        private Queue<MovementRecord> m_MoveRecordsFooted;
        private Queue<MovementRecord> m_MoveRecords;
        private static int m_OldCounterSize = 6;
        private int[,] m_OldMountedMomentRecordCount = new int[m_OldCounterSize,2];
        private int[,] m_OldFootedMomentRecordCount = new int[m_OldCounterSize,2];
        int m_InsertOldMomentRecordCountCounter;
        private DateTime m_EndQueue;
        private DateTime m_EndQueueMounted;
        private DateTime m_EndQueueFooted;
        private int m_FastWalkMaxStepInTick = 4; //Il valore è da vedere se 3 o 4 quando non sono su macchina virtual
        private int m_Sequence;
        private Direction oldDirection;
        private Direction oldDirectionFooted;
        private Direction oldDirectionMounted;
        bool m_MayBeFoot = false;
        bool m_Mounted;

        public int Sequence { get { return this.m_Sequence; } set { this.m_Sequence = value; } }

        public String Username { get { return this.m_Username; } }

        public FastWalkWithmount(String username)
        {
            this.m_Username = username;
            this.m_MoveRecordsMounted = new Queue<MovementRecord>(6);
            this.m_MoveRecordsFooted = new Queue<MovementRecord>(6);
            this.m_MoveRecords = new Queue<MovementRecord>(6);
            this.m_EndQueue = DateTime.Now;
            this.oldDirection = Direction.Down;
            this.m_InsertOldMomentRecordCountCounter = 0;
            initArray();
            m_Mounted = true;
        }

        private void initArray()
        {
            for (int i = 0; i < m_OldFootedMomentRecordCount.GetLength(0); i++)
            {
                m_OldFootedMomentRecordCount[i,0] = -1;
                m_OldMountedMomentRecordCount[i,0] = -1;
            }
        }

        public bool checkFastWalk(Direction d)
        {
            bool res = false;
            insertOldMovementRecord();
            SkipExpired(m_MoveRecordsMounted);
            SkipExpired(m_MoveRecordsFooted);
            SkipExpired(m_MoveRecords);

            
            checkFastWalk();

            TimeSpan delay = ComputeMovementSpeed(d);
            TimeSpan delayMounted = ComputeMovementSpeedMounted(d);
            TimeSpan delyaFooted = ComputeMovementSpeedFooted(d);

            DateTime end;
            DateTime endMounted;
            DateTime endFooted;

            if (m_MoveRecords.Count > 0)
                end = m_EndQueue + delay;
            else
                end = DateTime.Now + delay;
            m_EndQueue = end;

            if (m_MoveRecordsMounted.Count > 0)
                endMounted = m_EndQueueMounted + delayMounted;
            else
                endMounted = DateTime.Now + delayMounted;
            m_EndQueueMounted = endMounted;

            if (m_MoveRecordsFooted.Count > 0)
                endFooted = m_EndQueueFooted + delyaFooted;
            else
                endFooted = DateTime.Now + delyaFooted;
            m_EndQueueFooted = endFooted;

            m_MoveRecords.Enqueue(MovementRecord.NewInstance(end));
            m_MoveRecordsFooted.Enqueue(MovementRecord.NewInstance(endFooted));
            m_MoveRecordsMounted.Enqueue(MovementRecord.NewInstance(endMounted));

            
           
            
            return res;
        }

        private bool checkFastWalk()
        {
            if (m_MayBeFoot && average(m_OldFootedMomentRecordCount) > 3.0 && average(m_OldFootedMomentRecordCount) < 7)
            {
                Console.WriteLine("MAYBE");
            } 
            if (m_MoveRecords.Count >= m_FastWalkMaxStepInTick)
            {
                Console.WriteLine("FAST");
            }
            return false;
        }

        private double average(int[,] m_OldFootedMomentRecordCount)
        {
            int sum = 0;
            for (int i = 0; i < m_OldFootedMomentRecordCount.GetLength(0); i++)
            {
                sum += m_OldFootedMomentRecordCount[i, 0];
            }
            return (double)sum / (double)m_OldFootedMomentRecordCount.GetLength(0);
        }
        // TODO devo vedere come capire se sto a piedi o a cavallo sfruttando cambi di direzione e vecchie info sui counter
        private bool checkMount()
        {
            if (m_OldMountedMomentRecordCount[m_OldCounterSize - 1,0] == -1 || m_OldFootedMomentRecordCount[m_OldCounterSize - 1,0] == -1)
            {
                //DOBBIAMO ANALIZZARE PIU PACCHETTI
                return true;
            }
            bool footed = true, mounted = true;
            for (int i = 0; i < m_OldCounterSize; i++)
            {
               
                if (m_OldFootedMomentRecordCount[i,1] != -1)
                {
                    //SOLO CAMBI DI DIREZIONE QUINDI NON POSSO SAPERE
                    footed = false;
                    break;
                }
                if (m_OldMountedMomentRecordCount[i,1] != -1)
                {
                    //SOLO CAMBI DI DIREZIONE QUINDI NON POSSO SAPERE
                    mounted = false;
                    break;
                }
            }
            if (mounted == true && footed == true)
            {
                return true;
            }
            //SE MOUNTED SONO TUTTI 0 E NON SONO CAMBI DI DIREZIONE ALLORA POSSO STARE A PIEDI
            /*int zero = 0; int changeDirection= 0;
            for (int i = 0; i < m_OldCounterSize; i++)
            {
                if (m_OldMountedMomentRecordCount[i,0] == 0 )
                {
                    zero++;
                }
                if (m_OldMountedMomentRecordCount[i, 1] == -1)
                {
                    changeDirection++;
                }
                if (m_OldMountedMomentRecordCount[i, 0] > 0 && m_OldMountedMomentRecordCount[i, 1] != -1)
                {
                    return true;
                }
            }
            m_MayBeFoot = (zero != (m_OldMountedMomentRecordCount.GetLength(0) - changeDirection)) && zero != 0;
            return !(m_MayBeFoot && (max(m_OldFootedMomentRecordCount) < 4));*/
            if (max(m_OldMountedMomentRecordCount) == 1 && zeroPresent(m_OldMountedMomentRecordCount))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private int max(int[,] m_OldFootedMomentRecordCount)
        {
            int max = -1;
            for (int i = 0; i < m_OldFootedMomentRecordCount.GetLength(0); i++)
            {
                if (max < m_OldFootedMomentRecordCount[i, 0])
                {
                    max = m_OldFootedMomentRecordCount[i, 0];
                }
            }
            return max;
        }

        private bool zeroPresent(int[,] m_OldFootedMomentRecordCount)
        {
            bool res = false;
            for (int i = 0; i < m_OldFootedMomentRecordCount.GetLength(0) && !res; i++)
            {
                if (m_OldFootedMomentRecordCount[i, 0] == 0)
                {
                    res = true;
                }
            }
            return res;
        }

        private void insertOldMovementRecord()
        {
            m_OldFootedMomentRecordCount[m_InsertOldMomentRecordCountCounter,0] = m_MoveRecordsFooted.Count;
            m_OldMountedMomentRecordCount[m_InsertOldMomentRecordCountCounter,0] = m_MoveRecordsMounted.Count;
            if (m_InsertOldMomentRecordCountCounter == 5)
            {
                m_InsertOldMomentRecordCountCounter = -1;
            }
            m_InsertOldMomentRecordCountCounter++;
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
            m_Mounted = checkMount();
            if (m_Mounted)
            {
                Console.Write("MOUNT");
                return (dir & Direction.Running) != 0 ? m_RunMount : m_WalkMount;
                
            }
            else
            {
                Console.WriteLine("PIEDI");
                return (dir & Direction.Running) != 0 ? m_RunFoot : m_WalkFoot;
               
            }
        }

        private TimeSpan ComputeMovementSpeedMounted(Direction dir)
        {
            if ((dir & Direction.Mask) != (this.oldDirectionMounted & Direction.Mask))
            {
                this.oldDirectionMounted = dir;
                m_OldMountedMomentRecordCount[m_InsertOldMomentRecordCountCounter,1] = -1;
                return TimeSpan.FromSeconds(0.1);	// We are NOT actually moving (just a direction change)
                
            }
            m_OldMountedMomentRecordCount[m_InsertOldMomentRecordCountCounter, 1] = 1;
            bool running = ((dir & Direction.Running) != 0);
                return (dir & Direction.Running) != 0 ? m_RunMount : m_WalkMount;
        }

        private TimeSpan ComputeMovementSpeedFooted(Direction dir)
        {
            if ((dir & Direction.Mask) != (this.oldDirectionFooted & Direction.Mask))
            {
                this.oldDirectionFooted = dir;
                m_OldFootedMomentRecordCount[m_InsertOldMomentRecordCountCounter,1] = -1;
                return TimeSpan.FromSeconds(0.1);	// We are NOT actually moving (just a direction change)
            }
            m_OldMountedMomentRecordCount[m_InsertOldMomentRecordCountCounter, 1] = 1;
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
            if (m_MoveRecordsMounted != null && m_MoveRecordsMounted.Count > 0)
                m_MoveRecordsMounted.Clear();
            if (m_MoveRecordsFooted != null && m_MoveRecordsFooted.Count > 0)
                m_MoveRecordsFooted.Clear();
            if (m_MoveRecords != null && m_MoveRecords.Count > 0)
                m_MoveRecords.Clear();

            m_EndQueue = DateTime.Now;
            m_EndQueueFooted = DateTime.Now;
            m_EndQueueMounted = DateTime.Now;
        }
    }
}
