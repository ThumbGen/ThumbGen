<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt">
  <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
  <xsl:param name="IsEpisode" select="0"/>
  <xsl:param name="ExportBackdropsType" select="0"/>
  <!--xsl:template match="@*|node()">
      <xsl:copy>
         <xsl:apply-templates select="@*|node()"/>
      </xsl:copy>
   </xsl:template-->
  <xsl:template match="movie">
    <xsl:element name="details">
      <xsl:element name="title">
          <xsl:choose>
            <xsl:when test="$IsEpisode=0">
              <xsl:value-of select="title"/>    
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="episodename"/>
            </xsl:otherwise>
          </xsl:choose>
      </xsl:element>
      <xsl:element name="originaltitle">
        <xsl:value-of select="originaltitle"/>
      </xsl:element>
      <xsl:element name="overview">
        <xsl:choose>
          <xsl:when test="$IsEpisode=0">
            <xsl:value-of select="plot"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="episodeplot"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:element>
      <xsl:element name="year">
        <xsl:choose>
          <xsl:when test="$IsEpisode=0">
            <xsl:value-of select="releasedate"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="episodereleasedate"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:element>
      <xsl:element name="plot">
        <xsl:value-of select="plot"/>
      </xsl:element>
      <xsl:element name="runtime">
        <xsl:value-of select="runtime"/>
      </xsl:element>
      <xsl:element name="trailer">
        <xsl:value-of select="trailer"/>
      </xsl:element>
      <xsl:element name="rating">
        <xsl:value-of select="rating"/>
      </xsl:element>
      <xsl:element name="mpaa">
        <xsl:value-of select="certification"/>
      </xsl:element>
      <xsl:element name="imdb_id">
        <xsl:value-of select="id"/>
      </xsl:element>
      <xsl:element name="mpaa">
        <xsl:value-of select="certification"/>
      </xsl:element>
      <xsl:apply-templates select="genre"/>
      <xsl:apply-templates select="actor"/>
      <xsl:apply-templates select="director"/>
      <xsl:apply-templates select="studio"/>
      <xsl:apply-templates select="cover"/>
      <xsl:apply-templates select="backdrop"/>
      <xsl:element name="url">
        <xsl:attribute name="cache">
          <xsl:text>tmdb-.xml</xsl:text>
        </xsl:attribute>
        <xsl:attribute name="function">
          <xsl:text>GetTMDBThumbsById</xsl:text>
        </xsl:attribute>
      </xsl:element>
      <xsl:element name="prevtitle">
        <xsl:value-of select="filename"/>
      </xsl:element>
      <xsl:element name="prevgenre">
        <xsl:text>N/A</xsl:text>
      </xsl:element>
    </xsl:element>
  </xsl:template>
  <xsl:template match="genre">
    <xsl:for-each select="name">
      <xsl:element name="genre">
        <xsl:value-of select="."/>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>
  <xsl:template match="actor">
    <xsl:for-each select="name">
      <xsl:element name="actor">
        <xsl:element name="name">
          <xsl:value-of select=".">
          </xsl:value-of>
        </xsl:element>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>
  <xsl:template match="director">
    <xsl:for-each select="name">
      <xsl:element name="director">
        <xsl:value-of select="."/>
      </xsl:element>
    </xsl:for-each>
  </xsl:template>
  <xsl:template match="studio">
    <xsl:element name="studio">
      <xsl:for-each select="name">
        <xsl:value-of select="."/>
        <xsl:if test="position()!=last()">
          <xsl:text> / </xsl:text>
        </xsl:if>
      </xsl:for-each>
    </xsl:element>
  </xsl:template>
  <xsl:template match="cover">
    <xsl:choose>
      <xsl:when test="$ExportBackdropsType!=0">
        <xsl:for-each select="name">
          <xsl:element name="thumbnail">
            <xsl:value-of select="."/>
          </xsl:element>
        </xsl:for-each>    
      </xsl:when>
    </xsl:choose>
  </xsl:template>
  <xsl:template match="backdrop">
    <xsl:choose>
      <xsl:when test="$ExportBackdropsType!=0">
        <xsl:for-each select="name">
          <xsl:element name="backdrop">
            <xsl:value-of select="."/>
          </xsl:element>
        </xsl:for-each>
      </xsl:when>
    </xsl:choose>
  </xsl:template>

</xsl:stylesheet>
