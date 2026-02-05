using System;
using System.Collections.Generic;

namespace easySave_BMT.Resources_
{
    public static class ResourceManager
    {
        private static string currentLanguage = "fr";
        
        private static Dictionary<string, Dictionary<string, string>> resources = new Dictionary<string, Dictionary<string, string>>()
        {
            {
                "fr", new Dictionary<string, string>()
                {
                    { "DisplayBackups", "Afficher les sauvegardes" },
                    { "AddBackup", "Ajouter une sauvegarde" },
                    { "DeleteBackup", "Supprimer une sauvegarde" },
                    { "RunBackup", "Faire une backup" },
                    { "Configuration", "Configuration" },
                    { "Quit", "Quitter" },
                    { "Return", "Retour" },
                    { "MenuNavigation", "↑↓ pour naviguer | 1-6 ou ↵ pour valider | Échap pour annuler" },
                    { "DisplayConfig", "Afficher la configuration actuelle" },
                    { "ModifyLogDir", "Modifier le répertoire des logs" },
                    { "ModifyStateFile", "Modifier le fichier d'état" },
                    { "ChangeLanguage", "Changer la langue (fr/en)" },
                    { "CurrentConfiguration", "Configuration actuelle" },
                    { "LogDirectory", "Répertoire des logs" },
                    { "StateFile", "Fichier d'état" },
                    { "Language", "Langue" },
                    { "PressEnter", "Appuyez sur Entrée pour continuer..." },
                    { "LeaveEmptyToKeep", "Laissez vide pour garder la valeur actuelle." },
                    { "NewLogDirectory", "Nouveau répertoire des logs" },
                    { "InvalidPath", "Erreur: Chemin invalide." },
                    { "NoWritePermission", "Erreur: Pas de permission d'écriture sur cet emplacement." },
                    { "Error", "Erreur" },
                    { "ConfigStateFilePath", "Configuration du chemin du fichier d'état" },
                    { "CurrentStateFile", "Fichier d'état actuel" },
                    { "StateFilePathInstruction", "IMPORTANT : Fournissez un chemin d'accès complet incluant le nom du fichier (par ex. C:\\EasySave\\state.json)" },
                    { "NewStateFilePath", "Nouveau chemin du fichier d'état" },
                    { "StateFilePathWarning", "Avertissement : Le fichier doit avoir l'extension .json et inclure le nom complet du fichier." },
                    { "ConfigUpdated", "Configuration mise à jour avec succès !" },
                    { "PressEnterToMenu", "Appuyez sur Entrée pour afficher le menu . . . ." },
                    { "PressEnterMore", "Appuyer sur la touche Entrée pour en voir plus . . ." },
                    { "FileAddedSuccess", "Le fichier a été ajouté avec succès !!!" },
                    { "FileSavedSuccess", "Le fichier a été sauvegardé avec succès !" },
                    { "FileDeletedSuccess", "Le fichier a été supprimé avec succès !!!" },
                    { "BackupSuccess", "Backup réussi !" },
                    { "NoChanges", "Aucune modification depuis la dernière sauvegarde complète !\n" },
                    { "RestoreJSON", "Restaurer votre fichier de sauvegarde JSON." },
                    { "AddFailed", "Échec de l'ajout." },
                    { "SaveFailed", "Échec de sauvegarde." },
                    { "DeleteFailed", "Échec de la suppression." },
                    { "ListEmpty", "La liste est vide" },
                    { "ListFull", "La liste est pleine" },
                    { "InvalidOption", "Entrer une option valide" },
                    { "TransferFailed", "Échec du transfert d'un fichier, le fichier source ou de destination n'existe pas." },
                    { "BackupTypeNotExist", "Le type de sauvegarde sélectionné n'existe pas" },
                    { "CopyFailed", "Échec de la copie du fichier." },
                    { "CreateFolderFailed", "Échec de la création du dossier de sauvegarde." },
                    { "DirectoryNotExist", "Le répertoire n'existe pas. Veuillez entrer une source de répertoire valide." },
                    { "ChooseDifferentPath", "Choisissez un chemin différent de la source." },
                    { "DestinationNotExist", "Le répertoire n'existe pas. Veuillez entrer une destination de répertoire valide." },
                    { "NameTaken", "Le nom est déjà pris. Veuillez entrer un autre nom." },
                    { "EnterValidName", "Entrez un nom VALIDE (1 à 20 caractères):" },
                    { "BackupCompletedWithErrors", "Backup terminé avec erreurs." },
                    { "DestinationInsideSource", "La destination ne peut pas être à l'intérieur de la source." },
                    { "UnknownError", "Échec : Erreur inconnue." },
                    { "FullBackup", "Sauvegarde complète" },
                    { "DifferentialBackup", "Sauvegarde différentielle" },
                    { "BackupType", "Type de sauvegarde" },
                    { "Name", "Nom" },
                    { "Source", "Source" },
                    { "Destination", "Destination" },
                    { "Type", "Type" },
                    { "BackupList", "Liste des sauvegardes" },
                    { "BackupSettings", "Paramètre de sauvegarde" },
                    { "EnterName", "Entrer un nom (1 à 20 caractères):" },
                    { "EnterSourceDirectory", "Entrez la source du répertoire" },
                    { "EnterDestinationDirectory", "Entrer la destination du répertoire" },
                    { "Progress", "Progression" },
                    { "CurrentFile", "Fichier actuel" },
                    { "FilesRemaining", "Fichiers restants" },
                    { "SizeRemaining", "Taille restante" },
                    { "Completed", "terminé" },
                    { "Duration", "Durée" },
                    { "FailedForFile", "Échec pour le fichier" },
                    { "BackupAll", "Tout sauvegarder" },
                    { "LaunchBackup", "Lancer une sauvegarde" }
                }
            },
            {
                "en", new Dictionary<string, string>()
                {
                    { "DisplayBackups", "Display backups" },
                    { "AddBackup", "Add backup" },
                    { "DeleteBackup", "Delete backup" },
                    { "RunBackup", "Run backup" },
                    { "Configuration", "Configuration" },
                    { "Quit", "Quit" },
                    { "Return", "Return" },
                    { "MenuNavigation", "↑↓ to navigate | 1-6 or ↵ to confirm | Esc to cancel" },
                    { "DisplayConfig", "Display current configuration" },
                    { "ModifyLogDir", "Modify log directory" },
                    { "ModifyStateFile", "Modify state file" },
                    { "ChangeLanguage", "Change language (fr/en)" },
                    { "CurrentConfiguration", "Current configuration" },
                    { "LogDirectory", "Log directory" },
                    { "StateFile", "State file" },
                    { "Language", "Language" },
                    { "PressEnter", "Press Enter to continue..." },
                    { "LeaveEmptyToKeep", "Leave empty to keep current value." },
                    { "NewLogDirectory", "New log directory" },
                    { "InvalidPath", "Error: Invalid path." },
                    { "NoWritePermission", "Error: No write permission at this location." },
                    { "Error", "Error" },
                    { "ConfigStateFilePath", "State file path configuration" },
                    { "CurrentStateFile", "Current state file" },
                    { "StateFilePathInstruction", "IMPORTANT: Provide a complete path including the file name (e.g., C:\\EasySave\\state.json)" },
                    { "NewStateFilePath", "New state file path" },
                    { "StateFilePathWarning", "Warning: The file must have a .json extension and include the full file name." },
                    { "ConfigUpdated", "Configuration updated successfully!" },
                    { "PressEnterToMenu", "Press Enter to display the menu . . . ." },
                    { "PressEnterMore", "Press Enter to see more . . ." },
                    { "FileAddedSuccess", "File added successfully!!!" },
                    { "FileSavedSuccess", "File saved successfully!" },
                    { "FileDeletedSuccess", "File deleted successfully!!!" },
                    { "BackupSuccess", "Backup successful!" },
                    { "NoChanges", "No changes since the last full backup!\n" },
                    { "RestoreJSON", "Restore your JSON backup file." },
                    { "AddFailed", "Add failed." },
                    { "SaveFailed", "Save failed." },
                    { "DeleteFailed", "Delete failed." },
                    { "ListEmpty", "The list is empty" },
                    { "ListFull", "The list is full" },
                    { "InvalidOption", "Enter a valid option" },
                    { "TransferFailed", "File transfer failed, source or destination file does not exist." },
                    { "BackupTypeNotExist", "The selected backup type does not exist" },
                    { "CopyFailed", "File copy failed." },
                    { "CreateFolderFailed", "Backup folder creation failed." },
                    { "DirectoryNotExist", "Directory does not exist. Please enter a valid source directory." },
                    { "ChooseDifferentPath", "Choose a different path from the source." },
                    { "DestinationNotExist", "Directory does not exist. Please enter a valid destination directory." },
                    { "NameTaken", "Name is already taken. Please enter another name." },
                    { "EnterValidName", "Enter a VALID name (1 to 20 characters):" },
                    { "BackupCompletedWithErrors", "Backup completed with errors." },
                    { "DestinationInsideSource", "Destination cannot be inside source." },
                    { "UnknownError", "Failed: Unknown error." },
                    { "FullBackup", "Full backup" },
                    { "DifferentialBackup", "Differential backup" },
                    { "BackupType", "Backup type" },
                    { "Name", "Name" },
                    { "Source", "Source" },
                    { "Destination", "Destination" },
                    { "Type", "Type" },
                    { "BackupList", "Backup list" },
                    { "BackupSettings", "Backup settings" },
                    { "EnterName", "Enter a name (1 to 20 characters):" },
                    { "EnterSourceDirectory", "Enter source directory" },
                    { "EnterDestinationDirectory", "Enter destination directory" },
                    { "Progress", "Progress" },
                    { "CurrentFile", "Current file" },
                    { "FilesRemaining", "Files remaining" },
                    { "SizeRemaining", "Size remaining" },
                    { "Completed", "completed" },
                    { "Duration", "Duration" },
                    { "FailedForFile", "Failed for file" },
                    { "BackupAll", "Backup all" },
                    { "LaunchBackup", "Launch backup" }
                }
            }
        };

        public static void SetLanguage(string language)
        {
            if (resources.ContainsKey(language))
            {
                currentLanguage = language;
            }
        }

        public static string GetString(string key)
        {
            if (resources.ContainsKey(currentLanguage) && resources[currentLanguage].ContainsKey(key))
            {
                return resources[currentLanguage][key];
            }
            return key;
        }

        public static string GetCurrentLanguage()
        {
            return currentLanguage;
        }
    }
}