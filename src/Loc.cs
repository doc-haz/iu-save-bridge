using System;
using System.Collections.Generic;

namespace IUSaveBridge
{
    public static class Loc
    {
        public const string DefaultLanguage = "en";
        public const string SpanishLanguage = "es";

        private static string s_currentLanguage = DefaultLanguage;
        public static string CurrentLanguage
        {
            get { return s_currentLanguage; }
            private set { s_currentLanguage = value; }
        }

        private static readonly Dictionary<string, string> s_stringsEn = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> s_stringsEs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static Loc()
        {
            InitializeEnglish();
            InitializeSpanish();
        }

        public static void SetLanguage(string langCode)
        {
            if (string.Equals(langCode, "es", StringComparison.OrdinalIgnoreCase))
            {
                CurrentLanguage = SpanishLanguage;
            }
            else
            {
                CurrentLanguage = DefaultLanguage;
            }
        }

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";

            Dictionary<string, string> currentDict = (CurrentLanguage == SpanishLanguage) ? s_stringsEs : s_stringsEn;
            string value;
            if (currentDict.TryGetValue(key, out value))
            {
                return value;
            }

            // Fallback to English
            if (s_stringsEn.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }

        public static string Format(string key, params object[] args)
        {
            string fmt = Get(key);
            try
            {
                return string.Format(fmt, args);
            }
            catch
            {
                return fmt;
            }
        }

        /// <summary>Localized, human-friendly display name for a canonical profile code.</summary>
        public static string ProfileDisplay(string profileCode)
        {
            string code = SaveManager.NormalizeProfile(profileCode);
            if (string.Equals(code, SaveManager.ProfileUsa, StringComparison.OrdinalIgnoreCase)) return Get("ProfileUsa");
            if (string.Equals(code, SaveManager.ProfileUsaUndub, StringComparison.OrdinalIgnoreCase)) return Get("ProfileUsaUndub");
            if (string.Equals(code, SaveManager.ProfileEurope, StringComparison.OrdinalIgnoreCase)) return Get("ProfileEurope");
            if (string.Equals(code, SaveManager.ProfileJapan, StringComparison.OrdinalIgnoreCase)) return Get("ProfileJapan");
            if (string.Equals(code, SaveManager.ProfileAsia, StringComparison.OrdinalIgnoreCase)) return Get("ProfileAsia");
            return profileCode ?? "";
        }

        public static IEnumerable<string> GetAllKeys()
        {
            return s_stringsEn.Keys;
        }

        public static bool HasKey(string langCode, string key)
        {
            Dictionary<string, string> dict = (string.Equals(langCode, "es", StringComparison.OrdinalIgnoreCase)) ? s_stringsEs : s_stringsEn;
            return dict.ContainsKey(key);
        }

        private static void InitializeEnglish()
        {
            var d = s_stringsEn;

            // Application Header & Identity
            d["AppTitle"] = "Infinite Undiscovery Recomp Save Editor";
            d["AppSubtitle"] = "IU Save Bridge v2.3.0 • Official Save Companion";
            d["LangLabel"] = "Language:";
            d["ProfileLabel"] = "Profile:";
            d["ProfileNotInstalled"] = "(not installed)";
            d["ProfileUsa"] = "USA";
            d["ProfileUsaUndub"] = "USA UNDUB (Japanese Voices)";
            d["ProfileEurope"] = "Europe";
            d["ProfileJapan"] = "Japan";
            d["ProfileAsia"] = "Asia (English)";
            d["BadgeNoSave"] = "No Save Loaded";
            d["BadgeActiveSave"] = "Active Save: {0} / Slot {1}";

            // Tabs
            d["TabSaves"] = "  Saves  ";
            d["TabCharacters"] = "  Characters  ";
            d["TabInventory"] = "  Inventory  ";
            d["TabBackups"] = "  Backups  ";
            d["TabSettings"] = "  Settings  ";

            // Tab 1: Saves
            d["GbDetectedSaves"] = "Detected Save Slots";
            d["ColSlot"] = "Slot";
            d["ColFolderName"] = "Folder";
            d["ColModified"] = "Modified";
            d["ColFol"] = "Fol";
            d["ColCrc"] = "CRC Status";
            d["ColSha256"] = "SHA-256";
            d["BtnOpenSave"] = "Open Save";
            d["BtnRefreshSaves"] = "Refresh Slots";
            d["BtnOpenFolder"] = "Open Saves Folder";
            d["BtnSaveChanges"] = "Save Changes to Save File";
            d["GbActiveSlotDetails"] = "Active Save Details";
            d["LblActiveFileNone"] = "No save currently opened. Select a slot and click 'Open Save'.";
            d["LblActiveFile"] = "Active: {0}";
            d["LblFol"] = "Fol (Currency):";
            d["BtnUpdateFol"] = "Apply Fol";
            d["BtnMaxFol"] = "Max Fol (99,999,999)";
            d["LblNoThumbnail"] = "No Thumbnail";
            d["MsgSelectSlot"] = "Please select a save slot from the list.";
            d["MsgSaveOpened"] = "Loaded {0} Slot {1} ({2:N0} Fol).";
            d["BtnCreateBackupSlot"] = "Create Manual Backup";

            // Tab 2: Characters
            d["GbCharList"] = "Characters (18)";
            d["GbCharStats"] = "Character Attributes & Stats";
            d["NoSaveForChars"] = "No save file loaded. Please open a save from the Saves tab to edit characters.";
            d["CbInParty"] = "In Party";
            d["CbInActiveParty"] = "In Active Battle Party";
            d["LblLevel"] = "Level:";
            d["LblExp"] = "EXP:";
            d["LblCurHp"] = "Current HP:";
            d["LblMaxHp"] = "Max HP:";
            d["LblCurMp"] = "Current MP:";
            d["LblMaxMp"] = "Max MP:";
            d["LblAtk"] = "ATK:";
            d["LblDef"] = "DEF:";
            d["LblHit"] = "HIT:";
            d["LblAgl"] = "AGL:";
            d["LblInt"] = "INT:";
            d["LblAp"] = "AP:";
            d["BtnApplyChar"] = "Apply Character Changes";
            d["BtnMaxStats"] = "Max Stats (Safe)";
            d["CharTitleFormat"] = "{0} (Offset 0x{1:X6})";
            d["MsgCharApplied"] = "Character '{0}' updated in working memory. Remember to Save Changes!";

            // Tab 3: Inventory
            d["NoSaveForItems"] = "No save file loaded. Please open a save from the Saves tab to edit inventory.";
            d["LblSearch"] = "Search:";
            d["CbOnlyOwned"] = "Only owned items (> 0)";
            d["BtnItem99"] = "Selected x99";
            d["BtnSetOwned99"] = "Set Owned to 99";
            d["BtnAll99"] = "Give All 1,023 Items x99";
            d["BtnApplyItems"] = "Apply Inventory Changes";
            d["ColItemId"] = "ID";
            d["ColItemName"] = "Item Name";
            d["ColItemAmount"] = "Quantity (0-99)";
            d["MsgConfirmOwned99"] = "Are you sure you want to set all currently owned items to quantity 99?";
            d["MsgConfirmAll99"] = "WARNING: This will give you 99 of ALL 1,023 items in the game.\n\nAre you sure you want to continue?";
            d["MsgItemsApplied"] = "Inventory updated in working memory. Remember to Save Changes!";

            // Tab 4: Backups
            d["GbBackups"] = "Automated & Safety Backups";
            d["ColBkpFile"] = "Backup File";
            d["ColBkpProfile"] = "Profile";
            d["BackupLegacySuffix"] = " (legacy)";
            d["ColBkpSlot"] = "Slot";
            d["ColBkpDate"] = "Date & Time";
            d["ColBkpSize"] = "Size";
            d["ColBkpFol"] = "Fol";
            d["BtnRestoreBackup"] = "Restore Backup";
            d["BtnRefreshBackups"] = "Refresh List";
            d["BtnOpenBackupsFolder"] = "Open Backups Folder";
            d["MsgConfirmRestore"] = "Restore backup '{0}' to {1} Slot {2}?\n\nA safety backup of your CURRENT save will be created automatically before restoring.";
            d["MsgRestoreSuccess"] = "Backup restored successfully to Slot {0}!";

            // Tab 5: Settings
            d["GbRecomp"] = "Infinite Undiscovery Recomp Location";
            d["LblRecompPath"] = "Recomp Root Directory:";
            d["BtnChangeLocation"] = "Change Game Location";
            d["BtnOpenSaveDir"] = "Open Saves Directory";
            d["GbPreferences"] = "Application Preferences";
            d["LblPreferredProfile"] = "Active Save Profile:";
            d["LblPreferredLanguage"] = "Interface Language:";
            d["GbAbout"] = "About & Credits";
            d["AboutAppName"] = "Infinite Undiscovery Recomp Save Editor";
            d["AboutSubtitle"] = "IU Save Bridge v2.3.0 • Official Save Companion";
            d["AboutProject"] = "Recomp Project: https://github.com/doc-haz/infinite-undiscovery-recomp";
            d["AboutDesc"] = "Direct, safe save editor companion for Infinite Undiscovery Recomp.\nDesigned specifically for portable ReXGlue saves with automatic backups, high-integrity Tri-Ace CRC32 recalculation, and zero registry footprint.";

            // Log & Operation messages
            d["LogHeader"] = "Operation Log:";
            d["LogInitialized"] = "IU Save Bridge v2.3.0 initialized. Ready.";
            d["LogRecompDetected"] = "Recomp location set to: {0}";
            d["LogScanCompleted"] = "Scan completed. Found {0} save slot(s) for profile {1}.";
            d["LogSaveLoaded"] = "Opened save {0} (Fol: {1:N0}, SHA256: {2}).";
            d["LogSaveSaved"] = "Save written and verified successfully! Auto-backup created: {0}.";
            d["LogAutoBackupCreated"] = "Automatic backup created: {0}";
            d["LogBackupRestored"] = "Restored backup '{0}' to slot {1}.";
            d["LogFolUpdated"] = "Fol set to {0:N0}.";
            d["LogFolMax"] = "Fol set to maximum (99,999,999).";
            d["LogCharSaved"] = "Character '{0}' updated: Lv.{1}, HP {2}, ATK {3}.";
            d["LogInventorySaved"] = "Inventory changes updated.";
            d["LogAllItems99"] = "All 1,023 items set to quantity 99.";
            d["LogOwnedItems99"] = "Owned items set to quantity 99.";
            d["LogProfileChanged"] = "Active profile changed to: {0}.";
            d["MsgSaveSuccess"] = "Save changes written and verified successfully!\n\nAutomatic backup created:\n{0}";
            d["MsgBackupCreated"] = "Backup created successfully:\n{0}";
            d["MsgNoRecompFound"] = "Could not locate Infinite Undiscovery Recomp automatically.\nPlease select the folder where InfiniteUndiscoveryRecomp.exe is located.";
            d["MsgSelectRecompFolder"] = "Select Infinite Undiscovery Recomp Folder";
            d["MsgInvalidRecompFolder"] = "The selected folder does not appear to be an Infinite Undiscovery Recomp installation.";
            d["MetadataFormat"] = "Magic: 0x{0:X8} | Version: 0x{1:X8} | TitleID: 0x{2:X8} | CRC1: 0x{3:X8} ({4}) | CRC2: 0x{5:X8} ({6}) | SHA256: {7}";
            d["StatusValid"] = "Valid";
            d["StatusInvalid"] = "Invalid";

            // Common dialog titles
            d["TitleSuccess"] = "Success";
            d["TitleSaved"] = "Save Successful";
            d["TitleError"] = "Error";
            d["TitleNotice"] = "Notice";
            d["TitleConfirm"] = "Confirm Action";
            d["TitleBackupCreated"] = "Backup Created";
            d["TitleRestoreSuccess"] = "Restore Complete";
            d["TitleCharUpdated"] = "Character Updated";
            d["TitleInventorySaved"] = "Inventory Updated";
            d["FileFilterDat"] = "Infinite Undiscovery Save (*.dat)|*.dat|All Files (*.*)|*.*";
            d["FileFilterBin"] = "Infinite Undiscovery Save (*.bin;*.dat)|*.bin;*.dat|All Files (*.*)|*.*";

            // Menu & Import Xbox 360 Save
            d["MenuFile"] = "File";
            d["MenuFileImportXbox"] = "Import Xbox 360 Save...";
            d["MenuFileOpenSaveFolder"] = "Open Saves Directory";
            d["MenuFileExit"] = "Exit";
            d["MenuHelp"] = "Help";
            d["MenuHelpAbout"] = "About...";
            d["BtnImportXboxSave"] = "Import Xbox 360 Save...";
            d["ImportTitle"] = "Import Xbox 360 Save";
            d["ImportPreviewHeader"] = "Xbox 360 Save Detected";
            d["ImportSourceFile"] = "Source File:";
            d["ImportContainerType"] = "Container:";
            d["ImportTitleId"] = "Title ID:";
            d["ImportOrigSlot"] = "Original Slot:";
            d["ImportFol"] = "Fol:";
            d["ImportCapellLevel"] = "Capell Level:";
            d["ImportCrcStatus"] = "CRC Status:";
            d["ImportTargetProfile"] = "Target Profile:";
            d["ImportTargetUser"] = "Target User ID:";
            d["ImportTargetSlot"] = "Destination Slot:";
            d["ImportBtnAction"] = "Import Save";
            d["ImportBtnCancel"] = "Cancel";
            d["ImportConflictTitle"] = "Slot Already Exists";
            d["ImportConflictMsg"] = "Slot {0} already exists in {1} ({2}).\r\n\r\nDo you want to replace this slot? An automatic backup will be created before overwriting.";
            d["ImportConflictReplace"] = "Replace Existing Slot";
            d["ImportConflictChooseOther"] = "Choose Another Slot";
            d["ImportSuccessTitle"] = "Import Complete";
            d["ImportSuccessMsg"] = "Xbox 360 save imported successfully into Slot {0} ({1})!";
            d["ImportErrNotStfs"] = "The selected file is not a valid Xbox 360 STFS/CON container.";
            d["ImportErrTitleMismatch"] = "The container Title ID (0x{0:X8}) does not match Infinite Undiscovery (0x{1:X8}).";
            d["ImportErrNoPayload"] = "The file 'InfiniteUndiscovery.dat' was not found inside the Xbox 360 container.";
            d["ImportErrInvalidPayload"] = "Extracted save payload is invalid or corrupted.";
            d["ImportErrNoRecomp"] = "Infinite Undiscovery Recomp directory has not been configured.\nPlease set the game directory in Settings first.";
            d["FileFilterXboxSave"] = "Xbox 360 Save Files (*.bin;*.con;*.live;*.pirs;*.*)|*.bin;*.con;*.live;*.pirs;*.*|All Files (*.*)|*.*";
            d["ImportGbDetails"] = "Xbox 360 Save Details";
            d["ImportGbDestination"] = "Destination Settings (Recomp)";
            d["ImportCrcValid"] = "Valid (Match)";
            d["ImportCrcInvalid"] = "Error / Mismatch";
            d["ImportTitleIdVal"] = "0x{0:X8} (Infinite Undiscovery)";
            d["ImportSlotFormat"] = "Slot {0}";
            d["ImportFolFormat"] = "{0:N0} Fol";
            d["ImportCapellLvlFormat"] = "Lv. {0}";
            d["ImportSlotConflictHint"] = "[!] Slot {0} already exists in target destination.\r\nImporting will prompt to replace (with backup) or pick another slot.";
            d["ImportSlotAvailableHint"] = "[OK] Slot {0} is available for import.";
        }

        private static void InitializeSpanish()
        {
            var d = s_stringsEs;

            // Application Header & Identity
            d["AppTitle"] = "Infinite Undiscovery Recomp Save Editor";
            d["AppSubtitle"] = "IU Save Bridge v2.3.0 • Companion Oficial de Saves";
            d["LangLabel"] = "Idioma:";
            d["ProfileLabel"] = "Perfil:";
            d["ProfileNotInstalled"] = "(no instalado)";
            d["ProfileUsa"] = "USA";
            d["ProfileUsaUndub"] = "USA UNDUB (Voces japonesas)";
            d["ProfileEurope"] = "Europa";
            d["ProfileJapan"] = "Japón";
            d["ProfileAsia"] = "Asia (Inglés)";
            d["BadgeNoSave"] = "Ningún save cargado";
            d["BadgeActiveSave"] = "Save activo: {0} / Ranura {1}";

            // Tabs
            d["TabSaves"] = "  Partidas  ";
            d["TabCharacters"] = "  Personajes  ";
            d["TabInventory"] = "  Inventario  ";
            d["TabBackups"] = "  Copias de seguridad  ";
            d["TabSettings"] = "  Ajustes  ";

            // Tab 1: Saves
            d["GbDetectedSaves"] = "Ranuras de guardado detectadas";
            d["ColSlot"] = "Ranura";
            d["ColFolderName"] = "Carpeta";
            d["ColModified"] = "Modificado";
            d["ColFol"] = "Fol";
            d["ColCrc"] = "Estado CRC";
            d["ColSha256"] = "SHA-256";
            d["BtnOpenSave"] = "Abrir save";
            d["BtnRefreshSaves"] = "Refrescar ranuras";
            d["BtnOpenFolder"] = "Abrir carpeta de saves";
            d["BtnSaveChanges"] = "Guardar cambios en el save";
            d["GbActiveSlotDetails"] = "Detalles del save activo";
            d["LblActiveFileNone"] = "Ningún save abierto actualmente. Selecciona una ranura y pulsa 'Abrir save'.";
            d["LblActiveFile"] = "Activo: {0}";
            d["LblFol"] = "Fol (Dinero):";
            d["BtnUpdateFol"] = "Aplicar Fol";
            d["BtnMaxFol"] = "Máximo Fol (99,999,999)";
            d["LblNoThumbnail"] = "Sin miniatura";
            d["MsgSelectSlot"] = "Por favor, selecciona una ranura de guardado de la lista.";
            d["MsgSaveOpened"] = "Cargado {0} Ranura {1} ({2:N0} Fol).";
            d["BtnCreateBackupSlot"] = "Crear backup manual";

            // Tab 2: Characters
            d["GbCharList"] = "Personajes (18)";
            d["GbCharStats"] = "Atributos y estadísticas del personaje";
            d["NoSaveForChars"] = "Ningún save cargado. Abre una partida desde la pestaña Partidas para editar personajes.";
            d["CbInParty"] = "En el grupo";
            d["CbInActiveParty"] = "En grupo activo de batalla";
            d["LblLevel"] = "Nivel:";
            d["LblExp"] = "EXP:";
            d["LblCurHp"] = "HP actual:";
            d["LblMaxHp"] = "HP máx:";
            d["LblCurMp"] = "MP actual:";
            d["LblMaxMp"] = "MP máx:";
            d["LblAtk"] = "ATQ:";
            d["LblDef"] = "DEF:";
            d["LblHit"] = "PUN:";
            d["LblAgl"] = "AGI:";
            d["LblInt"] = "INT:";
            d["LblAp"] = "PA:";
            d["BtnApplyChar"] = "Aplicar cambios de personaje";
            d["BtnMaxStats"] = "Estadísticas máximas (seguro)";
            d["CharTitleFormat"] = "{0} (Offset 0x{1:X6})";
            d["MsgCharApplied"] = "¡Personaje '{0}' actualizado en memoria de trabajo! Recuerda pulsar Guardar cambios.";

            // Tab 3: Inventory
            d["NoSaveForItems"] = "Ningún save cargado. Abre una partida desde la pestaña Partidas para editar el inventario.";
            d["LblSearch"] = "Buscar:";
            d["CbOnlyOwned"] = "Solo items en posesión (> 0)";
            d["BtnItem99"] = "Seleccionado x99";
            d["BtnSetOwned99"] = "Poseídos a 99";
            d["BtnAll99"] = "Dar los 1.023 items x99";
            d["BtnApplyItems"] = "Aplicar cambios de inventario";
            d["ColItemId"] = "ID";
            d["ColItemName"] = "Nombre del item";
            d["ColItemAmount"] = "Cantidad (0-99)";
            d["MsgConfirmOwned99"] = "¿Estás seguro de que deseas poner todos los items en posesión a cantidad 99?";
            d["MsgConfirmAll99"] = "ADVERTENCIA: Esto otorgará 99 unidades de TODOS los 1.023 items del juego.\n\n¿Estás seguro de continuar?";
            d["MsgItemsApplied"] = "¡Inventario actualizado en memoria de trabajo! Recuerda pulsar Guardar cambios.";

            // Tab 4: Backups
            d["GbBackups"] = "Copias de seguridad automáticas";
            d["ColBkpFile"] = "Archivo de backup";
            d["ColBkpProfile"] = "Perfil";
            d["BackupLegacySuffix"] = " (heredado)";
            d["ColBkpSlot"] = "Ranura";
            d["ColBkpDate"] = "Fecha y hora";
            d["ColBkpSize"] = "Tamaño";
            d["ColBkpFol"] = "Fol";
            d["BtnRestoreBackup"] = "Restaurar copia";
            d["BtnRefreshBackups"] = "Refrescar lista";
            d["BtnOpenBackupsFolder"] = "Abrir carpeta de copias";
            d["MsgConfirmRestore"] = "¿Restaurar la copia '{0}' en {1} Ranura {2}?\n\nSe creará automáticamente un backup del save ACTUAL antes de restaurar.";
            d["MsgRestoreSuccess"] = "¡Copia restaurada con éxito en la Ranura {0}!";

            // Tab 5: Settings
            d["GbRecomp"] = "Ubicación de Infinite Undiscovery Recomp";
            d["LblRecompPath"] = "Directorio raíz del Recomp:";
            d["BtnChangeLocation"] = "Cambiar ubicación del juego";
            d["BtnOpenSaveDir"] = "Abrir directorio de saves";
            d["GbPreferences"] = "Preferencias de la aplicación";
            d["LblPreferredProfile"] = "Perfil de guardado activo:";
            d["LblPreferredLanguage"] = "Idioma de la interfaz:";
            d["GbAbout"] = "Acerca de y créditos";
            d["AboutAppName"] = "Infinite Undiscovery Recomp Save Editor";
            d["AboutSubtitle"] = "IU Save Bridge v2.3.0 • Companion Oficial de Saves";
            d["AboutProject"] = "Proyecto Recomp: https://github.com/doc-haz/infinite-undiscovery-recomp";
            d["AboutDesc"] = "Editor de partidas directo y seguro para Infinite Undiscovery Recomp.\nDiseñado específicamente para saves portables ReXGlue con backups automáticos, recálculo de CRC32 Tri-Ace de alta integridad y cero uso de registro.";

            // Log & Operation messages
            d["LogHeader"] = "Registro de operaciones:";
            d["LogInitialized"] = "IU Save Bridge v2.3.0 iniciado. Listo.";
            d["LogRecompDetected"] = "Ubicación de Recomp establecida en: {0}";
            d["LogScanCompleted"] = "Escaneo completado. Se encontraron {0} ranura(s) para el perfil {1}.";
            d["LogSaveLoaded"] = "Abierto save {0} (Fol: {1:N0}, SHA256: {2}).";
            d["LogSaveSaved"] = "¡Save guardado y verificado con éxito! Backup automático creado: {0}.";
            d["LogAutoBackupCreated"] = "Backup automático creado: {0}";
            d["LogBackupRestored"] = "Restaurada copia '{0}' en ranura {1}.";
            d["LogFolUpdated"] = "Fol cambiado a {0:N0}.";
            d["LogFolMax"] = "Fol cambiado al máximo (99,999,999).";
            d["LogCharSaved"] = "Personaje '{0}' actualizado: Nv.{1}, HP {2}, ATQ {3}.";
            d["LogInventorySaved"] = "Cambios de inventario actualizados.";
            d["LogAllItems99"] = "Todos los 1.023 items puestos a cantidad 99.";
            d["LogOwnedItems99"] = "Items en posesión puestos a cantidad 99.";
            d["LogProfileChanged"] = "Perfil activo cambiado a: {0}.";
            d["MsgSaveSuccess"] = "¡Cambios guardados y verificados con éxito!\n\nBackup automático creado:\n{0}";
            d["MsgBackupCreated"] = "Copia de seguridad creada con éxito:\n{0}";
            d["MsgNoRecompFound"] = "No se pudo localizar Infinite Undiscovery Recomp automáticamente.\nPor favor, selecciona la carpeta donde está InfiniteUndiscoveryRecomp.exe.";
            d["MsgSelectRecompFolder"] = "Seleccionar carpeta de Infinite Undiscovery Recomp";
            d["MsgInvalidRecompFolder"] = "La carpeta seleccionada no parece ser una instalación de Infinite Undiscovery Recomp.";
            d["MetadataFormat"] = "Magic: 0x{0:X8} | Version: 0x{1:X8} | TitleID: 0x{2:X8} | CRC1: 0x{3:X8} ({4}) | CRC2: 0x{5:X8} ({6}) | SHA256: {7}";
            d["StatusValid"] = "Válido";
            d["StatusInvalid"] = "Inválido";

            // Common dialog titles
            d["TitleSuccess"] = "Éxito";
            d["TitleSaved"] = "Guardado con éxito";
            d["TitleError"] = "Error";
            d["TitleNotice"] = "Aviso";
            d["TitleConfirm"] = "Confirmar acción";
            d["TitleBackupCreated"] = "Copia creada";
            d["TitleRestoreSuccess"] = "Restauración completada";
            d["TitleCharUpdated"] = "Personaje actualizado";
            d["TitleInventorySaved"] = "Inventario actualizado";
            d["FileFilterDat"] = "Partida Infinite Undiscovery (*.dat)|*.dat|Todos los archivos (*.*)|*.*";
            d["FileFilterBin"] = "Partida Infinite Undiscovery (*.bin;*.dat)|*.bin;*.dat|Todos los archivos (*.*)|*.*";

            // Menú e Importación de Save Xbox 360
            d["MenuFile"] = "Archivo";
            d["MenuFileImportXbox"] = "Importar save de Xbox 360...";
            d["MenuFileOpenSaveFolder"] = "Abrir carpeta de saves";
            d["MenuFileExit"] = "Salir";
            d["MenuHelp"] = "Ayuda";
            d["MenuHelpAbout"] = "Acerca de...";
            d["BtnImportXboxSave"] = "Importar save de Xbox 360...";
            d["ImportTitle"] = "Importar Save de Xbox 360";
            d["ImportPreviewHeader"] = "Save de Xbox 360 Detectado";
            d["ImportSourceFile"] = "Archivo origen:";
            d["ImportContainerType"] = "Contenedor:";
            d["ImportTitleId"] = "Title ID:";
            d["ImportOrigSlot"] = "Slot original:";
            d["ImportFol"] = "Fol:";
            d["ImportCapellLevel"] = "Nivel de Capell:";
            d["ImportCrcStatus"] = "Estado CRC:";
            d["ImportTargetProfile"] = "Perfil de destino:";
            d["ImportTargetUser"] = "ID de usuario destino:";
            d["ImportTargetSlot"] = "Slot de destino:";
            d["ImportBtnAction"] = "Importar save";
            d["ImportBtnCancel"] = "Cancelar";
            d["ImportConflictTitle"] = "Conflicto de Slot Detectado";
            d["ImportConflictMsg"] = "El Slot {0} ya existe en {1} ({2}).\r\n\r\n¿Deseas reemplazar este slot existente? (Se creará una copia de seguridad automática primero).";
            d["ImportConflictReplace"] = "Reemplazar slot existente";
            d["ImportConflictChooseOther"] = "Elegir otro slot";
            d["ImportSuccessTitle"] = "Importación Completada";
            d["ImportSuccessMsg"] = "¡El save de Xbox 360 se convirtió e importó correctamente al Slot {0} ({1})!";
            d["ImportErrNotStfs"] = "El archivo seleccionado no es un contenedor STFS/CON válido de Xbox 360.";
            d["ImportErrTitleMismatch"] = "El Title ID del contenedor (0x{0:X8}) no coincide con Infinite Undiscovery (0x{1:X8}).";
            d["ImportErrNoPayload"] = "No se encontró el archivo 'InfiniteUndiscovery.dat' dentro del contenedor de Xbox 360.";
            d["ImportErrInvalidPayload"] = "El payload del save extraído no es válido o está dañado.";
            d["ImportErrNoRecomp"] = "La carpeta de Infinite Undiscovery Recomp no está configurada.\nPor favor, establece la ruta del juego en Ajustes primero.";
            d["FileFilterXboxSave"] = "Saves de Xbox 360 (*.bin;*.con;*.live;*.pirs;*.*)|*.bin;*.con;*.live;*.pirs;*.*|Todos los archivos (*.*)|*.*";
            d["ImportGbDetails"] = "Detalles del Save de Xbox 360";
            d["ImportGbDestination"] = "Configuración de Destino (Recomp)";
            d["ImportCrcValid"] = "Válido (Coincide)";
            d["ImportCrcInvalid"] = "Error / No coincide";
            d["ImportTitleIdVal"] = "0x{0:X8} (Infinite Undiscovery)";
            d["ImportSlotFormat"] = "Ranura {0}";
            d["ImportFolFormat"] = "{0:N0} Fol";
            d["ImportCapellLvlFormat"] = "Nv. {0}";
            d["ImportSlotConflictHint"] = "[!] La ranura {0} ya existe en el destino.\r\nAl importar se pedirá reemplazar (con backup) o elegir otra ranura.";
            d["ImportSlotAvailableHint"] = "[OK] La ranura {0} está disponible para importar.";
        }
    }
}
