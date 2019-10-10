using System;
using System.Collections.Generic;
using System.Linq;

using DDictionary.Domain;
using DDictionary.Domain.Entities;


namespace DDictionary.DAL
{
    //https://www.youtube.com/watch?v=ayp3tHEkRc0

    public sealed class InMemoryMockStorage: IDBFacade
    {
#pragma warning disable CA1822 // Mark members as static

        private static readonly List<Clause> clauses = new List<Clause>();

        private static int clausesId;
        private static int translationsId;
        private static int relationsId;

        static InMemoryMockStorage()
        {
            var apple = new Clause {
                Id = ++clausesId,
                Sound = "https://audiocdn.lingualeo.com/v2/2/3256-631152008.mp3",
                Word = "apple",
                Transcription = "æpl",
                Translations = new List<Translation> {
                    new Translation {
                        Id = ++translationsId,
                        Part = PartOfSpeech.Noun,
                        Text = "яблоко"
                    },
                    new Translation {
                        Id = ++translationsId,
                        Part = PartOfSpeech.Adjective,
                        Text = "яблочный"
                    },
                },
                Context = "It's so delicious apple!",
                Relations = new List<Relation>(),
                Added = new DateTime(2019, 9, 1, 12, 0, 0, DateTimeKind.Local),
                Updated = new DateTime(2019, 9, 2, 12, 0, 0, DateTimeKind.Local),
                Group = WordGroup.A_DefinitelyKnown
            };

            var pear = new Clause {
                Id = ++clausesId,
                Sound = "https://audiocdn.lingualeo.com/v2/2/29775-631152008.mp3",
                Word = "pear",
                Transcription = "pɛə",
                Translations = new List<Translation> {
                    new Translation {
                        Id = ++translationsId,
                        Part = PartOfSpeech.Noun,
                        Text = "груша"
                    },
                },
                Context = "I hate pears!",
                Relations = new List<Relation>(),
                Added = new DateTime(2019, 9, 2, 12, 0, 0, DateTimeKind.Local),
                Updated = new DateTime(2019, 9, 3, 12, 0, 0, DateTimeKind.Local),
                Group = WordGroup.C_KindaKnown
            };

            var computer = new Clause {
                Id = ++clausesId,
                Sound = null,
                Word = "computer",
                Transcription = "kəm'pju:tə",
                Translations = new List<Translation> {
                    new Translation {
                        Id = ++translationsId,
                        Part = PartOfSpeech.Noun,
                        Text = "компьютер"
                    },
                    new Translation {
                        Id = ++translationsId,
                        Part = PartOfSpeech.Adjective,
                        Text = "компьютерный, машинный"
                    },
                },
                Context = "personal computer",
                Relations = new List<Relation>(),
                Added = new DateTime(2019, 9, 12, 12, 0, 0, DateTimeKind.Local),
                Updated = new DateTime(2019, 9, 13, 12, 0, 0, DateTimeKind.Local),
                Group = WordGroup.E_TotallyUnknown
            };

            ((List<Relation>)apple.Relations).Add(
                new Relation { Id = ++relationsId, From = apple, To = pear, Description = "Фрукты" });
            ((List<Relation>)apple.Relations).Add(
                new Relation { Id = ++relationsId, From = apple, To = computer, Description = "Apple Inc." });

            ((List<Relation>)computer.Relations).Add(
                new Relation { Id = ++relationsId, From = computer, To = apple, Description = "Apple Inc." });

            clauses.Add(apple);
            clauses.Add(pear);
            clauses.Add(computer);
        }

        public Clause GetClauseById(int id)
        {
            return clauses.SingleOrDefault(o => o.Id == id);
        }

        public IEnumerable<Clause> GetClauses(FiltrationCriteria filter = null)
        {
            if(filter is null)
                filter = new FiltrationCriteria(); //Empty filter - without filtration


            var ret = clauses.AsEnumerable();

            if(filter.RelatedFrom != null)
                ret = ret.Where(o => filter.RelatedFrom.Relations.Any(r => r.To.Id == o.Id));

            if(filter.ShownGroups?.Any() == true)
                ret = ret.Where(o => filter.ShownGroups.Contains(o.Group));

            if(!String.IsNullOrEmpty(filter.TextFilter))
            {
                //Primary search target (the word itself)
                var ret1 = ret.Where(o => o.Word.Contains(filter.TextFilter));

                //Secondary targets (excluding the word itself)
                var ret2 = ret.Where(o => !o.Word.Contains(filter.TextFilter) &&
                                          (o.Context.Contains(filter.TextFilter) ||
                                           o.Relations.Any(r => r.To.Word.Contains(filter.TextFilter)) ||
                                           o.Translations.Any(t => t.Text.Contains(filter.TextFilter))));

                //To get the words' matches in the beginning
                ret = ret1.Concat(ret2);
            }

            return ret;
        }

        public int GetTotalClauses()
        {
            return clauses.Count;
        }

        public IEnumerable<JustWordDTO> GetJustWords()
        {
            return clauses.Select(o => new JustWordDTO { Id = o.Id, Word = o.Word });
        }

        public void AddOrUpdateRelation(int relationId, int fromClauseId, int toClauseId, string relDescription)
        {
            Clause from = clauses.Single(o => o.Id == fromClauseId);
            Clause to = clauses.Single(o => o.Id == toClauseId);

            if(relationId == 0)
            { //New relation entity
                ((List<Relation>)from.Relations).Add(new Relation { 
                    Id = ++relationsId,
                    From = from,
                    To = to,
                    Description = relDescription
                });
            }
            else
            {
                Relation rel = from.Relations.Single(o => o.Id == relationId);
                rel.From = from;
                rel.To = to;
                rel.Description = relDescription;
            }

            from.Updated = DateTime.Now;
        }

        public void RemoveRelation(int relationId)
        {
            Clause cl = clauses.Single(o => o.Relations.Any(r => r.Id == relationId));
            
            ((List<Relation>)cl.Relations).Remove(cl.Relations.Single(r => r.Id == relationId));

            cl.Updated = DateTime.Now;
        }

#pragma warning restore CA1822 // Mark members as static
    }
}
