using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DDictionary.Domain;
using DDictionary.Domain.Entities;


namespace DDictionary.DAL
{
    public sealed class InMemoryMockStorage: IDBFacade
    {
#pragma warning disable CA1822 // Mark members as static

        private static readonly List<Clause> clauses = new List<Clause>();

        private static int clausesId;
        private static int translationsId;
        private static int relationsId;

        static InMemoryMockStorage()
        {
            int translationIdx = 0;
            var apple = new Clause {
                Id = ++clausesId,
                Sound = "https://audiocdn.lingualeo.com/v2/2/3256-631152008.mp3",
                Word = "apple",
                Transcription = "æpl",
                Translations = new List<Translation> {
                    new Translation {
                        Id = ++translationsId,
                        Index = ++translationIdx,
                        Part = PartOfSpeech.Noun,
                        Text = "яблоко"
                    },
                    new Translation {
                        Id = ++translationsId,
                        Index = ++translationIdx,
                        Part = PartOfSpeech.Adjective,
                        Text = "яблочный"
                    },
                },
                Context = "It's so delicious apple!",
                Relations = new List<Relation>(),
                Added = new DateTime(2019, 9, 1, 12, 0, 0, DateTimeKind.Local),
                Updated = new DateTime(2019, 9, 2, 12, 0, 0, DateTimeKind.Local),
                Watched = new DateTime(2019, 9, 2, 12, 0, 0, DateTimeKind.Local),
                Group = WordGroup.A_DefinitelyKnown
            };

            translationIdx = 0;
            var pear = new Clause {
                Id = ++clausesId,
                Sound = "https://audiocdn.lingualeo.com/v2/2/29775-631152008.mp3",
                Word = "pear",
                Transcription = "pɛə",
                Translations = new List<Translation> {
                    new Translation {
                        Id = ++translationsId,
                        Index = ++translationIdx,
                        Part = PartOfSpeech.Noun,
                        Text = "груша"
                    },
                },
                Context = "I hate pears!",
                Relations = new List<Relation>(),
                Added = new DateTime(2019, 9, 2, 12, 0, 0, DateTimeKind.Local),
                Updated = new DateTime(2019, 9, 3, 12, 0, 0, DateTimeKind.Local),
                Watched = new DateTime(2019, 9, 3, 12, 0, 0, DateTimeKind.Local),
                Group = WordGroup.C_KindaKnown
            };

            translationIdx = 0;
            var computer = new Clause {
                Id = ++clausesId,
                Sound = null,
                Word = "computer",
                Transcription = "kəm'pju:tə",
                Translations = new List<Translation> {
                    new Translation {
                        Id = ++translationsId,
                        Index = ++translationIdx,
                        Part = PartOfSpeech.Noun,
                        Text = "компьютер"
                    },
                    new Translation {
                        Id = ++translationsId,
                        Index = ++translationIdx,
                        Part = PartOfSpeech.Adjective,
                        Text = "компьютерный, машинный"
                    },
                },
                Context = "personal computer",
                Relations = new List<Relation>(),
                Added = new DateTime(2019, 9, 12, 12, 0, 0, DateTimeKind.Local),
                Updated = new DateTime(2019, 9, 13, 12, 0, 0, DateTimeKind.Local),
                Watched = new DateTime(2019, 9, 13, 12, 0, 0, DateTimeKind.Local),
                Group = WordGroup.E_TotallyUnknown
            };

            apple.Relations.Add(new Relation { Id = ++relationsId, ToClause = pear, Description = "Фрукты" });
            apple.Relations.Add(new Relation { Id = ++relationsId, ToClause = computer, Description = "Apple Inc." });
            
            computer.Relations.Add(new Relation { Id = ++relationsId, ToClause = apple, Description = "Apple Inc." });

            clauses.Add(apple);
            clauses.Add(pear);
            clauses.Add(computer);

            for(int i = 0; i < 5000; i++)
            {
                var clause = new Clause
                {
                    Id = ++clausesId,
                    Sound = "https://audiocdn.lingualeo.com/v2/2/29775-631152008.mp3",
                    Word = $"pear{i}",
                    Transcription = "pɛə",
                    Translations = new List<Translation> {
                        new Translation {
                            Id = ++translationsId,
                            Index = ++translationIdx,
                            Part = PartOfSpeech.Noun,
                            Text = "груша"
                        },
                    },
                    Context = "I hate pears!",
                    Relations = new List<Relation>(),
                    Added = new DateTime(2019, 9, 2, 12, 0, 0, DateTimeKind.Local),
                    Updated = new DateTime(2019, 9, 3, 12, 0, 0, DateTimeKind.Local),
                    Watched = new DateTime(2019, 9, 3, 12, 0, 0, DateTimeKind.Local),
                    Group = WordGroup.C_KindaKnown
                };

                clauses.Add(clause);
            }
        }

        public event ErrorHandler OnErrorOccurs;

        public Task<Clause> GetClauseByIdAsync(int id)
        {
            return Task.FromResult(clauses.SingleOrDefault(o => o.Id == id));
        }

        public Task<IEnumerable<Clause>> GetClausesAsync(FiltrationCriteria filter = null, 
            CancellationToken cancellationToken = default)
        {
            if(filter is null)
                filter = new FiltrationCriteria(); //Empty filter - without filtration

            var ret = clauses.AsEnumerable();

            if(filter.RelatedFrom != null)
                ret = ret.Where(o => filter.RelatedFrom.Relations.Any(r => r.ToClause.Id == o.Id));

            if(filter.ShownGroups?.Any() == true)
                ret = ret.Where(o => filter.ShownGroups.Contains(o.Group));

            if(!String.IsNullOrEmpty(filter.TextFilter))
            {
                //HACK: InMemoryMockStorage does not handle placeholders for filtration!

                const StringComparison sc = StringComparison.OrdinalIgnoreCase;

                //Primary search target (the word itself - the beginning is matched), in alphabet order
                var ret1 = ret.Where(o => o.Word.IndexOf(filter.TextFilter, sc) == 0)
                              .OrderBy(o => o.Word);

                //Secondary target (the word itself except primary target - matched but not the beginning)
                var ret2 = ret.Where(o => o.Word.IndexOf(filter.TextFilter, sc) > 0);

                //Tertiary target (relations, excluding the word itself)
                var ret3 = ret.Where(o => o.Word.IndexOf(filter.TextFilter, sc) < 0 &&
                                          o.Relations.Any(r => r.ToClause.Word.IndexOf(filter.TextFilter, sc) >= 0));

                //Quaternary targets (excluding all previous targets)
                var ret4 = ret.Where(o => o.Word.IndexOf(filter.TextFilter, sc) < 0 && 
                                          !o.Relations.Any(r => r.ToClause.Word.IndexOf(filter.TextFilter, sc) >= 0) &&
                                          (o.Context.IndexOf(filter.TextFilter, sc) >= 0 ||
                                           o.Translations.Any(t => t.Text.IndexOf(filter.TextFilter, sc) >= 0) ) );

                //To get the words' matches in the beginning
                ret = ret1.Concat(ret2).Concat(ret3).Concat(ret4);
            }

            return Task.FromResult(ret);
        }

        public Task<int> GetTotalClausesAsync()
        {
            return Task.FromResult(clauses.Count);
        }

        public Task<IEnumerable<JustWordDTO>> GetJustWordsAsync()
        {
            return Task.FromResult(clauses.Select(o => new JustWordDTO { Id = o.Id, Word = o.Word }));
        }

        public Task<int> AddOrUpdateRelationAsync(int relationId, int fromClauseId, int toClauseId, string relDescription)
        {
            Clause from = clauses.Single(o => o.Id == fromClauseId);
            Clause to = clauses.Single(o => o.Id == toClauseId);
            Relation ret = null;

            if(relationId == 0)
            { //New relation entity
                ret = new Relation {
                    Id = ++relationsId,
                    ToClause = to,
                    Description = relDescription
                };

                from.Relations.Add(ret);
            }
            else
            {
                ret = from.Relations.Single(o => o.Id == relationId);
                ret.ToClause = to;
                ret.Description = relDescription;
            }

            from.Updated = DateTime.Now;

            return Task.FromResult(ret.Id);
        }

        public Task RemoveRelationsAsync(params int[] relationIds)
        {
            foreach(var relation in relationIds)
                RemoveRelation(relation);

            return Task.CompletedTask;
        }

        private Task RemoveRelation(int relationId)
        {
            Clause cl = clauses.Single(o => o.Relations.Any(r => r.Id == relationId));
            
            cl.Relations.Remove(cl.Relations.Single(r => r.Id == relationId));

            cl.Updated = DateTime.Now;

            return Task.CompletedTask;
        }

        public Task RemoveClausesAsync(params int[] clauseIds)
        {
            foreach(var clause in clauseIds)
                RemoveClause(clause);

            return Task.CompletedTask;
        }

        private Task RemoveClause(int id)
        {
            Clause clauseToRemove = clauses.Single(o => o.Id == id);

            foreach(Relation relation in clauses.SelectMany(o => o.Relations).Where(o => o.ToClause.Id == clauseToRemove.Id).ToArray())
                RemoveRelation(relation.Id);

            clauses.Remove(clauseToRemove);

            return Task.CompletedTask;
        }

        public Task MoveClausesToGroupAsync(WordGroup toGroup, params int[] clauseIds)
        {
            foreach(var id in clauseIds)
                clauses.Single(o => o.Id == id).Group = toGroup;

            return Task.CompletedTask;

            //The group changing isn't counted as clause's modification so the last update date shouldn't be changed
        }

        public Task<int> AddOrUpdateTranslationAsync(Translation translation, int toClauseId)
        {
            Clause cl = clauses.Single(o => o.Id == toClauseId);
            Translation ret = null;

            if(translation.Id == 0)
            {
                ret = new Translation {
                    Id = ++translationsId,
                    Index = translation.Index,
                    Part = translation.Part,
                    Text = translation.Text
                };

                cl.Translations.Add(ret);
            }
            else
            {
                ret = cl.Translations.Single(o => o.Id == translation.Id);

                ret.Index = translation.Index;
                ret.Part = translation.Part;
                ret.Text = translation.Text;
            }

            cl.Updated = DateTime.Now;

            return Task.FromResult(ret.Id);
        }

        public Task RemoveTranslationsAsync(params int[] translationIds)
        {
            foreach(var translation in translationIds)
                RemoveTranslation(translation);

            return Task.CompletedTask;
        }

        private Task RemoveTranslation(int translationId)
        {
            Clause cl = clauses.Single(o => o.Translations.Any(tr => tr.Id == translationId));

            cl.Translations.Remove(cl.Translations.Single(tr => tr.Id == translationId));

            cl.Updated = DateTime.Now;

            return Task.CompletedTask;
        }

        public Task<int> AddOrUpdateClauseAsync(ClauseUpdateDTO clause, bool updateWatched)
        {
            DateTime now = DateTime.Now;

            Clause cl = clause.Id == 0 ? new Clause { Id = ++clausesId, Added = now } 
                                       : clauses.Single(o => o.Id == clause.Id);

            cl.Context = clause.Context;
            cl.Group = clause.Group;
            cl.Sound = clause.Sound;
            cl.Transcription = clause.Transcription;
            cl.Word = clause.Word;
            cl.Updated = now;
            
            if(clause.Id == 0 || updateWatched)
                cl.Watched = now;

            if(clause.Id == 0)
                clauses.Add(cl);
            else if(updateWatched)
                cl.WatchedCount += 1;

            return Task.FromResult(cl.Id);
        }

        public Task<int> UpdateClauseWatchAsync(int id)
        {
            Clause cl = clauses.Single(o => o.Id == id);
            
            cl.Watched = DateTime.Now;
            cl.WatchedCount += 1;

            return Task.FromResult(cl.WatchedCount);
        }

#pragma warning restore CA1822 // Mark members as static
    }
}
