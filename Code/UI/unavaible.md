# SCSS/CSS Non-Compatible avec Razor Engine de s&box

Voici la liste des propriétés et syntaxes CSS/SCSS qui ne sont pas compatibles avec l'engine Razor de s&box, d'après les erreurs rencontrées :

## Propriétés non supportées ou problématiques

- `background: rgba(...)` (utiliser `background-color` à la place)
- `overflow: auto` (préférer `overflow-x` ou `overflow-y` séparément, mais attention aux conflits)
- `overflow-y: auto` (en conflit avec `overflow: auto`)
- `overflow-wrap: break-word` ou `overflow-wrap: anywhere` (aucune valeur n'est acceptée)
- `word-break: break-word` (non supporté)
- `word-wrap: break-word` (non supporté)
- `border-style: dashed` ou `border-style: solid` (aucune valeur n'est acceptée)
- `border: 2px dashed #xxxxxx` (non supporté)
- `border-style` tout court (semble non reconnu)
- `user-select`, `will-change` (non supportés)

## Recommandations

- Utiliser uniquement `border-width` et `border-color` pour les bordures (le style par défaut sera solide).
- Pour un effet "dashed", utiliser une image de fond ou un SVG.
- Éviter les propriétés CSS avancées ou récentes, privilégier les propriétés de base.
- Tester chaque propriété individuellement pour vérifier la compatibilité.

---

Ce fichier doit être mis à jour si d'autres incompatibilités sont découvertes.