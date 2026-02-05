## EasySave-BMT

EasySave-BMT est une petite application console en C# qui permet de créer et d’exécuter des jobs de sauvegarde de dossiers.
L’objectif est d’avoir un outil simple à lancer en ligne de commande, avec un suivi d’état en temps réel et une journalisation propre des opérations.

### Fonctionnalités principales

- **Gestion de jobs de sauvegarde**: création, liste et suppression de jobs (jusqu’à 5 sauvegardes configurées).
- **Deux types de sauvegarde**: `FULL` (complète) et `DIFFERENTIAL` (copie uniquement des fichiers nouveaux/modifiés par rapport à la dernière sauvegarde complète).
- **Interface console interactive**: menus avec navigation au clavier (flèches, chiffres, Entrée, Échap).
- **Suivi en temps réel**: progression globale, nombre de fichiers restants, taille restante, barre de progression.
- **Journalisation détaillée**: un log est écrit pour chaque fichier copié (nom du backup, source, destination, taille, temps de transfert).
- **Fichier d’état JSON**: sauvegarde de l’état des jobs en temps réel, pour pouvoir reprendre ou analyser les sauvegardes.
- **Configuration utilisateur**: choix du dossier de logs, du chemin du fichier d’état et de la langue (fr/en).

### Prérequis

- **.NET SDK**: `net9.0` (ou plus récent compatible).
- **OS**: testée sous Windows (chemins en `\` et encodage console UTF‑8).

### Récupérer et lancer le projet

- **Clonage du dépôt** (ou récupération classique du projet).
- Depuis la racine du dépôt (`EasySave-BMT-1`), vous pouvez simplement :

```bash
dotnet build
dotnet run --project EasySave-BMT.csproj
```

L’application démarre dans la console et affiche le menu principal.

### Utilisation rapide

Au lancement, EasySave-BMT recharge les sauvegardes depuis `BackupSave.json` s’il existe, puis affiche un menu principal :

- **1 – Afficher les sauvegardes**: liste les jobs avec nom, source, destination et type.
- **2 – Ajouter une sauvegarde**:
  - saisie du nom (1–20 caractères, unique),
  - dossier source,
  - dossier de destination (ne doit pas être identique à la source ni inclus dedans),
  - type de sauvegarde (`FULL` ou `DIFFERENTIAL`).
- **3 – Supprimer une sauvegarde**: choix dans la liste puis suppression du job.
- **4 – Lancer les sauvegardes**:
  - lancer toutes les sauvegardes,
  - ou en sélectionner une seule.
- **5 – Configuration**:
  - afficher la configuration courante,
  - modifier le dossier de logs,
  - modifier le chemin du fichier d’état (JSON),
  - changer la langue (fr / en).
- **6 – Quitter**: fermeture propre de l’application.

Pendant une sauvegarde, l’écran affiche en haut :

- **Nom du backup**;
- **Fichier en cours** (taille formatée en ko/Mo/Go…);
- **Nombre de fichiers restants**;
- **Taille restante**;
- une **barre de progression** mise à jour après chaque fichier.

En fin de sauvegarde, un récapitulatif affiche la durée totale, la progression à 100 %, ainsi que la liste des fichiers éventuellement en erreur.

### Structure du projet

- **`Program`**: point d’entrée, initialise le `ViewModel` et lance `RunApp()`.
- **`ViewModel`**:
  - gère la logique applicative et le lien entre la `View` et le `Model`,
  - prépare les listes de fichiers à copier (full ou différentiel),
  - orchestre le déroulement d’un backup (calcul de taille totale, temps, gestion des erreurs).
- **`Model`**:
  - charge et sauvegarde la configuration (`Config`),
  - persiste la liste des sauvegardes dans `BackupSave.json`,
  - gère l’écriture dans le fichier d’état (`RealTimeState`) et les logs (`EasyLogger`),
  - effectue la copie physique des fichiers.
- **`View`**:
  - gère toute l’interface console (menus, saisies, affichage des messages et de la progression),
  - utilise un système de ressources (`Resources/Strings.resx`, `Strings.fr.resx`) pour les textes.
- **`EasyLog`**:
  - petite bibliothèque interne pour la journalisation des opérations de sauvegarde (`LogEntry`, `EasyLogger`).

L’architecture suit un schéma proche MVVM, adapté à une application console : `View` pour l’affichage, `ViewModel` pour la logique et `Model` pour les données et la persistance.

### Fichiers générés importants

- **`BackupSave.json`**: contient la liste des jobs de sauvegarde configurés.
- **Fichiers de log** dans le dossier de logs configuré: une entrée par fichier copié, avec horodatage et temps de transfert.
- **Fichier d’état JSON** (chemin configurable): permet de suivre en temps réel l’avancement des sauvegardes.

### Conventions de commit de l’équipe

Pour garder un historique de commits lisible, le projet utilise des **conventional commits**.

- **Format**:

```bash
git commit -m "<type>(<scope optionnel>): <description>"
```

- **Types autorisés** (principaux) :
  - **feat**: ajout, modification ou suppression d’une fonctionnalité côté API ou interface.
  - **fix**: correction de bug lié à une fonctionnalité existante.
  - **refactor**: réécriture / restructuration du code sans changer le comportement fonctionnel.
  - **perf**: refactor orienté amélioration de performance.
  - **style**: changements de style uniquement (formatage, espaces, renommage mineur…) sans impact fonctionnel.
  - **test**: ajout ou mise à jour de tests automatisés.
  - **docs**: modifications de documentation uniquement.

L’idée est que le type et, si besoin, le scope permettent de comprendre rapidement la nature du changement sans avoir à lire tout le diff.