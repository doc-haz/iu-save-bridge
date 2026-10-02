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
            d["AppSubtitle"] = "IU Save Bridge v2.2.0 • Official Save Companion";
            d["LangLabel"] = "Language:";
            d["RegionLabel"] = "Region:";
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
            d["ColBkpRegion"] = "Region";
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
            d["LblPreferredRegion"] = "Active Save Region:";
            d["LblPreferredLanguage"] = "Interface Language:";
            d["GbAbout"] = "About & Credits";
            d["AboutAppName"] = "Infinite Undiscovery Recomp Save Editor";
            d["AboutSubtitle"] = "IU Save Bridge v2.2.0 • Official Save Companion";
            d["AboutProject"] = "Recomp Project: https://github.com/doc-haz/infinite-undiscovery-recomp";
            d["AboutDesc"] = "Direct, safe save editor companion for Infinite Undiscovery Recomp.\nDesigned specifically for portable ReXGlue saves with automatic backups, high-integrity Tri-Ace CRC32 recalculation, and zero registry footprint.";

            // Log & Operation messages
            d["LogHeader"] = "Operation Log:";
            d["LogInitialized"] = "IU Save Bridge v2.2.0 initialized. Ready.";
            d["LogRecompDetected"] = "Recomp location set to: {0}";
            d["LogScanCompleted"] = "Scan completed. Found {0} save slot(s) for region {1}.";
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
            d["LogRegionChanged"] = "Active region changed to: {0}.";
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
        }

        private static void InitializeSpanish()
        {
            var d = s_stringsEs;

            // Application Header & Identity
            d["AppTitle"] = "Infinite Undiscovery Recomp Save Editor";
            d["AppSubtitle"] = "IU Save Bridge v2.2.0 • Companion Oficial de Saves";
            d["LangLabel"] = "Idioma:";
            d["RegionLabel"] = "Región:";
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
            d["ColBkpRegion"] = "Región";
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
            d["LblPreferredRegion"] = "Región de guardado activa:";
            d["LblPreferredLanguage"] = "Idioma de la interfaz:";
            d["GbAbout"] = "Acerca de y créditos";
            d["AboutAppName"] = "Infinite Undiscovery Recomp Save Editor";
            d["AboutSubtitle"] = "IU Save Bridge v2.2.0 • Companion Oficial de Saves";
            d["AboutProject"] = "Proyecto Recomp: https://github.com/doc-haz/infinite-undiscovery-recomp";
            d["AboutDesc"] = "Editor de partidas directo y seguro para Infinite Undiscovery Recomp.\nDiseñado específicamente para saves portables ReXGlue con backups automáticos, recálculo de CRC32 Tri-Ace de alta integridad y cero uso de registro.";

            // Log & Operation messages
            d["LogHeader"] = "Registro de operaciones:";
            d["LogInitialized"] = "IU Save Bridge v2.2.0 iniciado. Listo.";
            d["LogRecompDetected"] = "Ubicación de Recomp establecida en: {0}";
            d["LogScanCompleted"] = "Escaneo completado. Se encontraron {0} ranura(s) para la región {1}.";
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
            d["LogRegionChanged"] = "Región activa cambiada a: {0}.";
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
        }
    }
}
