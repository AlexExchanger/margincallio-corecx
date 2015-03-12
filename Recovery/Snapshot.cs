using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using CoreCX.Trading;

namespace CoreCX.Recovery
{
    static class Snapshot
    {
        internal static StatusCodes BackupCore(bool local)
        {
            if (local) //локальный снэпшот ядра (запись на диск)
            {
                try
                {
                    //backup ID
                    using (StreamWriter sw = new StreamWriter(new FileStream(@"recovery\ids.dat", FileMode.Create, FileAccess.Write, FileShare.None)))
                    {
                        sw.WriteLine(FuncCall.next_id);
                        sw.WriteLine(Order.next_id);
                        sw.WriteLine(Trade.next_id);
                    }

                    //backup Core
                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream fs = new FileStream(@"recovery\core.bin", FileMode.Create, FileAccess.Write, FileShare.None);
                    formatter.Serialize(fs, App.core);
                    fs.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(DateTime.Now + " ==BACKUP CORE SNAPSHOT ERROR==");
                    Console.WriteLine(e.ToString());
                    return StatusCodes.ErrorSnapshotBackupFailed;
                }

                Console.WriteLine(DateTime.Now + " CORE: snapshot saved to disk (local backup)");
                return StatusCodes.Success;
            }
            else //удалённый снэпшот ядра
            {
                                
                return StatusCodes.Success;
            }
        }

        internal static StatusCodes RestoreCore(bool local)
        {
            if (local) //локальный снэпшот ядра (чтение с диска)
            {
                try
                {
                    //restore ID
                    using (StreamReader sr = new StreamReader(new FileStream(@"recovery\ids.dat", FileMode.Open, FileAccess.Read, FileShare.Read)))
                    {
                        FuncCall.next_id = long.Parse(sr.ReadLine());
                        Order.next_id = long.Parse(sr.ReadLine());
                        Trade.next_id = long.Parse(sr.ReadLine());
                    }

                    //restore Core
                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream fs = new FileStream(@"recovery\core.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
                    App.core = (Core)formatter.Deserialize(fs);
                    fs.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(DateTime.Now + " ==RESTORE CORE SNAPSHOT ERROR==");
                    Console.WriteLine(e.ToString());
                    return StatusCodes.ErrorSnapshotRestoreFailed;
                }

                Console.WriteLine(DateTime.Now + " CORE: snapshot retrieved from disk (local restore)");
                return StatusCodes.Success;
            }
            else //удалённый снэпшот ядра
            {
                
                return StatusCodes.Success;
            }
        }

    }
}
