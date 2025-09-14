using Common;
using System;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for periodic polling.
    /// </summary>
    public class Acquisitor : IDisposable
	{
		private AutoResetEvent acquisitionTrigger;
        private IProcessingManager processingManager;
        private Thread acquisitionWorker;
		private IStateUpdater stateUpdater;
		private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Acquisitor"/> class.
        /// </summary>
        /// <param name="acquisitionTrigger">The acquisition trigger.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="stateUpdater">The state updater.</param>
        /// <param name="configuration">The configuration.</param>
		public Acquisitor(AutoResetEvent acquisitionTrigger, IProcessingManager processingManager, IStateUpdater stateUpdater, IConfiguration configuration)
		{
			this.stateUpdater = stateUpdater;
			this.acquisitionTrigger = acquisitionTrigger;
			this.processingManager = processingManager;
			this.configuration = configuration;
			this.InitializeAcquisitionThread();
			this.StartAcquisitionThread();
		}

		#region Private Methods

        /// <summary>
        /// Initializes the acquisition thread.
        /// </summary>
		private void InitializeAcquisitionThread()
		{
			this.acquisitionWorker = new Thread(Acquisition_DoWork);
			this.acquisitionWorker.Name = "Acquisition thread";
		}

        /// <summary>
        /// Starts the acquisition thread.
        /// </summary>
		private void StartAcquisitionThread()
		{
			acquisitionWorker.Start();
		}

        /// <summary>
        /// Acquisitor thread logic.
        /// </summary>
		private void Acquisition_DoWork()
        {
            while (true)
            {
                // Čeka signal (npr. timer ili neki drugi thread) da krene novi ciklus akvizicije.
                // Na ovaj način thread ne radi "busy loop", nego čeka da ga neko probudi.
                acquisitionTrigger.WaitOne();

                // Uzimamo sve konfiguracione stavke koje treba periodično očitavati.
                var items = this.configuration.GetConfigurationItems();

                foreach (var item in items)
                {
                    // Ako je prošlo dovoljno ciklusa (SecondsPassedSinceLastPoll >= AcquisitionInterval),
                    // vreme je da očitamo podatke za ovu stavku.
                    if (item.SecondsPassedSinceLastPoll >= item.AcquisitionInterval)
                    {
                        try
                        {
                            // Šalje se komanda za čitanje registra sa uređaja.
                            // Parametri dolaze iz konfiguracije i same stavke (adresa, broj registara...).
                            this.processingManager.ExecuteReadCommand(
                                item,                                    // Konfiguraciona stavka (IConfigItem)
                                this.configuration.GetTransactionId(),   // ID transakcije
                                this.configuration.UnitAddress,          // Adresa uređaja
                                item.StartAddress,                       // Start adresa registra
                                item.NumberOfRegisters                   // Broj registara za čitanje
                            );
                        }
                        catch (Exception)
                        {
                            // Ako dođe do greške pri čitanju, za sada samo ispišemo poruku u konzolu.
                            // (ovde se može ubaciti logovanje ili obaveštavanje stateUpdater-a).
                            Console.WriteLine("Error executing read command.");
                        }

                        // Resetujemo brojač jer smo upravo izvršili akviziciju.
                        item.SecondsPassedSinceLastPoll = 0;
                    }
                    else
                    {
                        // Ako još nije vreme za akviziciju, samo povećavamo brojač.
                        item.SecondsPassedSinceLastPoll++;
                    }
                }
            }
        }




        //TO DO: IMPLEMENT za ovo iznad instrukcije 
        // petlja kroz konfiguraciju
        // items = this.configuration.getconfigurationitema()
        // for tiem in items
        // 
        // if item.brojac == item.aquisition interval
        //aquisitiontrigger wait one()
        // proccessingManager.executereadcommand(i, 
        // this.configuration.gettransacionId();, this.configuration.unitAddress,
        // i.startAddress, i.numberOfRegisters);
        // items.brojac = 0;
        //}
        // item brojac++;


        #endregion Private Methods

        /// <inheritdoc />
        public void Dispose()
		{
			acquisitionWorker.Abort();
        }
	}
}