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
                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream fs = new FileStream("Core.bin", FileMode.Create, FileAccess.Write, FileShare.None);
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
                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream fs = new FileStream("Core.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
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
