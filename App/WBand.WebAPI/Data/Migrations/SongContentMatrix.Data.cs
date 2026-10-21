using Microsoft.EntityFrameworkCore.Migrations;

namespace WBand.WebAPI.Data.Migrations;

public partial class SongContentMatrix
{
    private static void MigrateContent(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        create temporary table restored_partition_ids (id uuid primary key) on commit drop;
        do $$
        begin
            -- Fresh upgrades preserve the original section IDs, names and both text fields exactly.
            if to_regclass('public.song_matrix_legacy_sections') is not null then
                create temporary table original_axes on commit drop as
                    select distinct on (song_id) song_id, gen_random_uuid() as lyrics_id, gen_random_uuid() as chords_id,
                        created_at, created_by
                    from public.song_matrix_legacy_sections order by song_id, "order", created_at, id;
                insert into public.song_sections
                    (id, song_id, name, "order", revision, created_at, created_by, updated_at, updated_by)
                    select id, song_id, name, "order", revision, created_at, created_by, updated_at, updated_by
                    from public.song_matrix_legacy_sections;
                insert into public.song_partitions
                    (id, song_id, name, "order", content, revision, created_at, created_by)
                    select lyrics_id, song_id, 'Слова', 0, '', 1, created_at, created_by from original_axes
                    union all
                    select chords_id, song_id, 'Аккорды', 1, '', 1, created_at, created_by from original_axes;
                insert into restored_partition_ids select lyrics_id from original_axes union all select chords_id from original_axes;
                insert into public.song_content_cells
                    (id, song_id, section_id, partition_id, content, revision, created_at, created_by, updated_at, updated_by)
                    select gen_random_uuid(), section.song_id, section.id, cell.partition_id, cell.content,
                        section.revision, section.created_at, section.created_by, section.updated_at, section.updated_by
                    from public.song_matrix_legacy_sections section
                    join original_axes axis on axis.song_id = section.song_id
                    cross join lateral (values (axis.lyrics_id, section.lyrics), (axis.chords_id, section.chords)) cell(partition_id, content)
                    where cell.content is not null;
                delete from public.song_partitions where id in (select id from public.song_matrix_legacy_partitions);
                drop table public.song_matrix_legacy_sections, public.song_matrix_legacy_partitions;
            end if;
        end $$;

        -- Also upgrade databases that already applied the flat-partition version.
        -- Only the known migration prefixes identify old sections; custom parts retain their names
        -- and their entire content is placed into one shared "Песня" row.
        create temporary table flat_parts on commit drop as
            select part.*,
                case when name like 'Слова — %' then 1 when name like 'Аккорды — %' then 2 else 0 end as kind,
                case when name like 'Слова — %' then substring(name from 9)
                    when name like 'Аккорды — %' then substring(name from 11) end as section_name
            from public.song_partitions part where id not in (select id from restored_partition_ids);
        create temporary table flat_rows on commit drop as
            select part.*,
                case when kind = 2 and previous_kind = 1 and section_name = previous_name
                    and created_at = previous_created_at and created_by = previous_created_by
                    and revision = previous_revision and updated_at is not distinct from previous_updated_at
                    and updated_by is not distinct from previous_updated_by
                    then previous_id else id end as row_id,
                case when kind > 0 then first_value(id) over (partition by song_id, kind order by "order", created_at, id)
                    else id end as axis_id
            from (select part.*,
                lag(id) over ordered as previous_id, lag(kind) over ordered as previous_kind,
                lag(section_name) over ordered as previous_name, lag(created_at) over ordered as previous_created_at,
                lag(created_by) over ordered as previous_created_by, lag(revision) over ordered as previous_revision,
                lag(updated_at) over ordered as previous_updated_at, lag(updated_by) over ordered as previous_updated_by
                from flat_parts part window ordered as (partition by song_id order by "order", created_at, id)) part;
        create temporary table default_rows on commit drop as
            select distinct on (song_id) gen_random_uuid() as id, song_id, "order", created_at, created_by
                from flat_parts where kind = 0 order by song_id, "order", created_at, id;
        insert into public.song_sections
            (id, song_id, name, "order", revision, created_at, created_by, updated_at, updated_by)
            select distinct on (row_id) row_id, song_id, section_name, "order", revision, created_at, created_by, updated_at, updated_by
                from flat_rows where kind > 0 order by row_id, "order", created_at, id;
        insert into public.song_sections (id, song_id, name, "order", revision, created_at, created_by)
            select id, song_id, 'Песня', "order", 1, created_at, created_by from default_rows;
        insert into public.song_content_cells
            (id, song_id, section_id, partition_id, content, revision, created_at, created_by, updated_at, updated_by)
            select gen_random_uuid(), part.song_id, case when kind = 0 then defaults.id else row_id end,
                axis_id, content, revision, part.created_at, part.created_by, part.updated_at, part.updated_by
            from flat_rows part left join default_rows defaults on defaults.song_id = part.song_id;
        delete from public.song_partitions where id in (select id from flat_rows where id <> axis_id);
        update public.song_partitions part set name = case when rows.kind = 1 then 'Слова' else 'Аккорды' end,
            "order" = rows.kind - 1
            from flat_rows rows where rows.id = rows.axis_id and rows.id = part.id and rows.kind > 0;
        """);
}
