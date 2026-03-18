# 🎯 Convert Remaining VB Files in Features\VisualBasic\Portable to C#

## Understanding
Continue the VB to C# conversion for the remaining 205 files across 20+ subdirectories in `src\Features\VisualBasic\Portable`. The previous plan covered initial directories and SignatureHelp (26 files). This plan addresses all remaining VB files systematically.

## Assumptions
- Follow same conversion patterns: file-scoped namespaces, primary constructors where applicable
- Maintain all functionality - these files still provide VB language support, just implemented in C#
- Delete original .vb files after successful conversion
- Build verification deferred until all conversions complete

## Approach
Process remaining directories in descending order by file count to maximize progress visibility. Batch similar-sized directories together for efficiency. Large directories (>15 files) get dedicated steps; smaller ones are grouped.

## Key Files
**Remaining directories with file counts:**
- Highlighting (31 files)
- CodeFixes (17 files)  
- ExtractMethod (16 files)
- EditAndContinue (14 files)
- CodeRefactorings (10 files)
- Debugging (8 files)
- EmbeddedLanguages (7 files) - additional files beyond initial batch
- Plus 13 smaller directories (6 or fewer files each)

## Risks & Open Questions
- 205 files is substantial work; may require multiple sessions
- Some CodeFixes and Refactorings may have complex VB-specific patterns
- EditAndContinue files may have special debugging considerations
- Build verification must happen before marking complete

**Progress**: 0% [░░░░░░░░░░]

**Last Updated**: 2026-03-18 00:57:54

## 📝 Plan Steps
-  **Convert Highlighting directory files (31 files)**
-  **Convert CodeFixes directory files - batch 1 (first 10 files)**
-  **Convert CodeFixes directory files - batch 2 (remaining 7 files)**
-  **Convert ExtractMethod directory files (16 files)**
-  **Convert EditAndContinue directory files (14 files)**
-  **Convert CodeRefactorings directory files (10 files)**
-  **Convert Debugging directory files (8 files)**
-  **Convert EmbeddedLanguages directory remaining files (7 files)**
-  **Convert medium-sized directories: IntroduceVariable (6), Diagnostics (6), SplitOrMergeIfStatements (5), Organizing (5)**
-  **Convert small directories batch 1: LanguageServices (4), InvertIf (3), QuickInfo (3), ReplacePropertyWithMethods (3)**
-  **Convert small directories batch 2: ConvertIfToSwitch (3), FullyQualify (2), SpellCheck (2), InitializeParameter (2), Formatting (2), and remaining misc files**
-  **Verify all VB files converted**
-  **Run build verification**
-  **Final review and cleanup**

