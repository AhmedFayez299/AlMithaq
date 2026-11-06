// Run manually after reviewing upstream licenses and changes. No dependencies or API keys.
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const base = path.resolve(__dirname, '../Resources/Raw');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const urls = {
  uthmani: 'https://tanzil.net/pub/download/index.php?quranType=uthmani&outType=txt-2&agree=true',
  simple: 'https://tanzil.net/pub/download/index.php?quranType=simple-clean&outType=txt-2&agree=true',
  metadata: 'https://api.alquran.cloud/v1/quran/quran-uthmani'
};
async function get(url) { const response = await fetch(url); if (!response.ok) throw Error(`HTTP ${response.status}`); return response.text(); }
async function main() {
  const uthmani = await get(urls.uthmani), simple = await get(urls.simple), metadata = await get(urls.metadata);
  const original = uthmani.split(/\r?\n/).filter(l => /^\d+\|\d+\|/.test(l));
  const searchable = simple.split(/\r?\n/).filter(l => /^\d+\|\d+\|/.test(l));
  if (original.length !== 6236 || searchable.length !== 6236) throw Error('Unexpected Quran verse count');
  const api = JSON.parse(metadata); if (api.code !== 200 || api.data.surahs.length !== 114) throw Error('Invalid metadata');
  let index = 0;
  const chapters = api.data.surahs.map(s => ({ Number:s.number, Name:s.name, EnglishName:s.englishName,
    Revelation:s.revelationType === 'Meccan' ? 'مكية' : 'مدنية', Verses:s.ayahs.map(a => {
      const u=original[index].split('|'), q=searchable[index].split('|'); index++;
      if (+u[0] !== s.number || +u[1] !== a.numberInSurah || u[0] !== q[0] || u[1] !== q[1] || a.number !== index)
        throw Error('Verse identity mismatch');
      if (a.page < 1 || a.page > 604 || a.juz < 1 || a.juz > 30) throw Error('Invalid page/juz');
      return {Number:a.number,Surah:s.number,Ayah:a.numberInSurah,Page:a.page,Juz:a.juz,Text:u.slice(2).join('|'),SearchText:q.slice(2).join('|')};
    }) }));
  if (index !== 6236 || !uthmani.includes('# PLEASE') || !simple.includes('# PLEASE')) throw Error('Missing source license or verses');
  const notice = uthmani.slice(uthmani.indexOf('# PLEASE'));
  const json = JSON.stringify({Source:'https://tanzil.net',Version:'Tanzil Uthmani 1.1',CopyrightNotice:notice,
    SearchCopyrightNotice:simple.slice(simple.indexOf('# PLEASE')),MetadataSource:urls.metadata,Chapters:chapters});
  await fs.mkdir(base,{recursive:true});
  await fs.writeFile(path.join(base,'quran-uthmani-source.txt'),uthmani);
  await fs.writeFile(path.join(base,'quran-simple-source.txt'),simple);
  await fs.writeFile(path.join(base,'Tanzil-LICENSE.txt'),notice);
  await fs.writeFile(path.join(base,'quran.json'),json);
  await fs.writeFile(path.join(base,'quran-provenance.json'),JSON.stringify({urls,textSha256:hash(uthmani),searchSha256:hash(simple),
    metadataSha256:hash(metadata),jsonSha256:hash(json),verses:6236,chapters:114,downloadDate:new Date().toISOString()},null,2));
  console.log('Imported unchanged Quran text and separate search corpus. Run CoreChecks and inspect diff before shipping.');
}
main().catch(error => { console.error(error); process.exitCode=1; });
