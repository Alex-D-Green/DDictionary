using System;

using DDictionary.Domain.DTO;
using DDictionary.Domain.Entities;


namespace DDictionary.Presentation.Converters
{
    public static class RelationToRelationDTOMapper
    {
        public static RelationDTO MapToRelationDTO(this Relation rel)
        {
            if(rel is null)
                throw new ArgumentNullException(nameof(rel));

            return new RelationDTO {
                Id = rel.Id,
                ToWordId = rel.ToClause.Id,
                ToWord = rel.ToClause.Word,
                Description = rel.Description
            };
        }
    }
}
